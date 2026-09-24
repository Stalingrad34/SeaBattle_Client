using System;
using Colyseus.Schema;
using Game.Scripts.Multiplayer.Generated;
using UniRx;

using Game.Scripts.Infrastructure.Core.Configs;
using Game.Scripts.Infrastructure.Core.Network;

using Game.Scripts.Infrastructure.Implementations.Services;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public sealed class ArchitectureTests
    {
        [Test]
        public void SchemaRejectsWrongPlayerAndPublishesInPlacePatches()
        {
            var session = new SessionService();
            session.BeginConnection("m", "a");
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

    }
}
