using System;
using Colyseus.Schema;
using Game.Scripts.Multiplayer.Generated;
using UniRx;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Game.Scripts.Infrastructure.Core.Configs;
using Game.Scripts.Infrastructure.Core.Network;
using Game.Scripts.Infrastructure.Core.States;
using Game.Scripts.Infrastructure.Implementations;
using Game.Scripts.Infrastructure.Implementations.Services;
using NUnit.Framework;
using UnityEngine;
using Zenject;

namespace Game.Tests
{
    public sealed class ArchitectureTests
    {
        [Test]
        public void SchemaRejectsWrongPlayerAndPublishesInPlacePatches()
        {
            var session = new SessionService();
            session.AcceptWelcome(Welcome("a"));
            using var match = new MatchService(session);
            Assert.That(match.Apply(Snapshot("b", 8)), Is.False);
            var accepted = Snapshot("a", 2);
            Assert.That(match.Apply(accepted), Is.True);
            var notifications = 0;
            using var subscription = match.State.Subscribe(_ => notifications++);
            Assert.That(match.State.Value.revision, Is.EqualTo(2));
            Assert.That(match.Apply(Snapshot("a", 1)), Is.False);
            Assert.That(match.Apply(Snapshot("a", 2)), Is.False);
            accepted.revision = 3;
            Assert.That(match.Apply(accepted), Is.True);
            Assert.That(notifications, Is.EqualTo(2));
        }

        [Test]
        public void ProtocolRejectsMissingVersionAndUnknownMessages()
        {
            Assert.Throws<FormatException>(() => Protocol.Decode("{\"type\":\"snapshot\"}"));
            Assert.Throws<FormatException>(() => Protocol.Decode("{\"protocolVersion\":1,\"type\":\"secret\"}"));
        }

        [Test]
        public void ProtocolPreservesCommandIdentity()
        {
            var json = Protocol.Encode(new ClientMessage { type = Protocol.Fire, commandId = "cmd", turnId = 17, x = 4, y = 5 });
            var request = JsonUtility.FromJson<ClientMessage>(json);
            Assert.That(request.commandId, Is.EqualTo("cmd"));
            Assert.That(request.turnId, Is.EqualTo(17));
        }

        [Test]
        public async Task StateTransitionCancelsPreviousLifetimeAndExitsOnce()
        {
            var root = StateContainer();
            using var machine = root.Resolve<StateMachine>();
            await machine.EnterAsync<FirstState>(CancellationToken.None);
            var first = root.Resolve<FirstState>();
            Assert.That(first.Token.IsCancellationRequested, Is.False);
            await machine.EnterAsync<SecondState>(CancellationToken.None);
            Assert.That(first.Token.IsCancellationRequested, Is.True);
            Assert.That(first.ExitCount, Is.EqualTo(1));
            Assert.That(machine.CurrentType, Is.EqualTo(typeof(SecondState)));
            machine.Dispose();
            Assert.That(root.Resolve<SecondState>().ExitCount, Is.EqualTo(1));
        }

        [Test]
        public async Task NextStateCancelsAnInProgressAsyncEnter()
        {
            var root = StateContainer();
            root.Bind<WaitingState>().AsSingle();
            using var machine = root.Resolve<StateMachine>();
            var first = machine.EnterAsync<WaitingState>(CancellationToken.None).AsTask();
            var next = machine.EnterAsync<SecondState>(CancellationToken.None).AsTask();
            try
            {
                await first;
                Assert.Fail("Expected cancellation");
            }
            catch (OperationCanceledException)
            {
            }

            await next;
            Assert.That(root.Resolve<WaitingState>().ExitCount, Is.EqualTo(1));
            Assert.That(machine.CurrentType, Is.EqualTo(typeof(SecondState)));
        }

        [Test]
        public void InvalidConfigurationIsRejectedBeforeConnecting()
        {
            var game = ScriptableObject.CreateInstance<GameConfig>();
            var network = ScriptableObject.CreateInstance<ConnectionConfig>();
            try
            {
                game.Validate();
                network.Validate();
                game.ShipLengths = new[]
                {
                    7
                };
                Assert.Throws<InvalidOperationException>(() => game.Validate());
                network.LossProbability = 2;
                Assert.Throws<InvalidOperationException>(() => network.Validate());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(game);
                UnityEngine.Object.DestroyImmediate(network);
            }
        }

        [Test]
        public void ServerClockNeverShowsNegativeRemainingTime()
        {
            var time = new ServerTimeService();
            time.Synchronize(10000, 20);
            Assert.That(time.NowMs, Is.GreaterThanOrEqualTo(10010));
            Assert.That(time.RemainingSeconds(5000), Is.Zero);
        }

        private static DiContainer StateContainer()
        {
            var root = new DiContainer();
            root.Bind<StateFactory>().AsSingle();
            root.Bind<StateMachine>().AsSingle();
            root.Bind<FirstState>().AsSingle();
            root.Bind<SecondState>().AsSingle();
            return root;
        }

        private static ServerMessage Welcome(string player)
        {
            return new()
            {
                type = Protocol.Welcome,
                matchId = "m",
                playerId = player,
                connectionId = "generation",
                resumeToken = "test-token"
            };
        }

        private static MatchState Snapshot(string player, long revision)
        {
            var state = new MatchState
            {
                matchId = "m",
                revision = revision,
                players = new MapSchema<PlayerState>()
            };
            state.players[player] = new PlayerState
            {
                playerId = player
            };
            return state;
        }

        public class FirstState : IState
        {
            public CancellationToken Token;
            public int ExitCount;
            public virtual UniTask EnterAsync(CancellationToken token)
            {
                Token = token;
                return UniTask.CompletedTask;
            }

            public void Exit()
            {
                ExitCount++;
            }
        }

        public sealed class SecondState : FirstState
        {
        }

        public sealed class WaitingState : FirstState
        {
            public override async UniTask EnterAsync(CancellationToken token)
            {
                Token = token;
                await Task.Delay(Timeout.Infinite, token);
            }
        }
    }
}
