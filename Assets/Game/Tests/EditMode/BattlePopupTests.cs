using System;
using System.Collections.Generic;
using System.Threading;
using Colyseus.Schema;
using Cysharp.Threading.Tasks;
using Game.Scripts.Infrastructure.Core.Network;
using Game.Scripts.Infrastructure.Core.Services;
using Game.Scripts.Infrastructure.Implementations.Services;
using Game.Scripts.Infrastructure.Implementations.UI.Popups.BattlePopup;
using Game.Scripts.Multiplayer.Generated;
using NUnit.Framework;
using UniRx;
using UnityEngine;

namespace Game.Tests
{
    public sealed class BattlePopupTests
    {
        private FakeTransport _transport;
        private FakeTime _time;
        private MatchService _match;
        private BattlePopupModel _model;
        private ConnectionConfig _config;
        private SessionService _session;

        [SetUp]
        public void SetUp()
        {
            var session = _session = new SessionService();
            session.BeginConnection("TEST0001", "a");
            _transport = new FakeTransport { CurrentState = Snapshot(1, "a") };
            _time = new FakeTime();
            _match = new MatchService(session);
            _config = ScriptableObject.CreateInstance<ConnectionConfig>();
            _model = new BattlePopupModel(null, _transport, _match, session, _time, null, _config, null);
        }

        [TearDown]
        public void TearDown()
        {
            _model.Dispose();
            _match.Dispose();
            _transport.Dispose();
            UnityEngine.Object.DestroyImmediate(_config);
        }

        [Test]
        public void FastClicksSendOnceAndAppliedReplyWaitsForSchema()
        {
            _model.FireAsync(0, 0).GetAwaiter().GetResult();
            _model.FireAsync(1, 0).GetAwaiter().GetResult();
            Assert.That(_transport.SendCount, Is.EqualTo(1));
            _transport.Results.OnNext(Result("applied"));
            Assert.That(_model.Pending.Value, Is.Not.Null);
            Assert.That(_model.CanFire.Value, Is.False);
            _transport.States.OnNext(Snapshot(2, "b", true));
            Assert.That(_model.Pending.Value, Is.Null);
            Assert.That(_model.Feedback.Value, Is.EqualTo("Промах"));
            Assert.That(_model.CanFire.Value, Is.False);
        }

        [Test]
        public void SchemaBeforeReplyAndUnrelatedReplyDoNotUnlockPendingShot()
        {
            _model.FireAsync(0, 0).GetAwaiter().GetResult();
            _transport.States.OnNext(Snapshot(2, "b", true));
            var unrelated = Result("applied");
            unrelated.commandId = "other";
            _transport.Results.OnNext(unrelated);
            Assert.That(_model.Pending.Value, Is.Not.Null);
            _transport.Results.OnNext(Result("applied"));
            Assert.That(_model.Pending.Value, Is.Null);
            _transport.States.OnNext(Snapshot(3, "a", true));
            Assert.That(_model.CanShootCell(0, 0), Is.False);
            Assert.That(_model.CanShootCell(1, 0), Is.True);
        }

        [Test]
        public void RejectedShotDoesNotPaintBoardAndCanBeRetried()
        {
            _model.FireAsync(0, 0).GetAwaiter().GetResult();
            _transport.Results.OnNext(Result("rejected"));
            Assert.That(_model.Pending.Value, Is.Null);
            Assert.That(_match.State.Value.players["a"].outgoingShots.Count, Is.Zero);
            Assert.That(_model.CanShootCell(0, 0), Is.True);
            _model.FireAsync(1, 0).GetAwaiter().GetResult();
            Assert.That(_transport.SendCount, Is.EqualTo(2));
        }

        [Test]
        public void MissingReplyStaysBlockedAndLateReplyCanResolveIt()
        {
            _model.FireAsync(0, 0).GetAwaiter().GetResult();
            _time.NowMs = 11000;
            _model.Tick();
            Assert.That(_model.Feedback.Value, Does.Contain("неизвестен"));
            Assert.That(_model.CanFire.Value, Is.False);
            _transport.Results.OnNext(Result("applied"));
            _transport.States.OnNext(Snapshot(2, "b", true));
            Assert.That(_model.Pending.Value, Is.Null);
        }

        [Test]
        public void ExpiredRoomKeepsUnknownShotBlockedAndShowsMenuInstruction()
        {
            _model.FireAsync(0, 0).GetAwaiter().GetResult();
            _transport.Connection.Value = ConnectionStatus.SessionExpired;
            _time.NowMs = 20000;
            _model.Tick();
            _model.FireAsync(1, 0).GetAwaiter().GetResult();
            Assert.That(_model.Pending.Value, Is.Not.Null);
            Assert.That(_model.CanFire.Value, Is.False);
            Assert.That(_transport.SendCount, Is.EqualTo(1));
            Assert.That(_model.Status.Value, Is.EqualTo("Партия больше недоступна"));
            Assert.That(_model.Feedback.Value, Does.Contain("Вернитесь в меню"));
        }

        [Test]
        public void ReconnectionRepeatsSamePendingCommandAndWaitsForAuthoritativeState()
        {
            _model.FireAsync(0, 0).GetAwaiter().GetResult();
            var command = _transport.LastCommand;
            Assert.That(_session.Recovery.pending, Is.SameAs(command));
            _transport.Connection.Value = ConnectionStatus.Reconnecting;
            _model.FireAsync(1, 0).GetAwaiter().GetResult();
            Assert.That(_transport.SendCount, Is.EqualTo(1));
            Assert.That(_model.CanFire.Value, Is.False);
            _transport.Connection.Value = ConnectionStatus.Connected;
            Assert.That(_transport.SendCount, Is.EqualTo(2));
            Assert.That(_transport.LastCommand, Is.SameAs(command));
            _transport.Results.OnNext(Result("applied"));
            Assert.That(_session.Recovery.pending, Is.Not.Null);
            _transport.States.OnNext(Snapshot(2, "b", true));
            Assert.That(_session.Recovery.pending, Is.Null);
        }

        [Test]
        public void RecreatedModelResendsUnresolvedShotWithOriginalId()
        {
            _model.FireAsync(0, 0).GetAwaiter().GetResult();
            var command = _transport.LastCommand;
            _model.Dispose();
            _model = new BattlePopupModel(null, _transport, _match, _session, _time, null, _config, null);
            Assert.That(_transport.SendCount, Is.EqualTo(2));
            Assert.That(_transport.LastCommand.commandId, Is.EqualTo(command.commandId));
            Assert.That(_model.CanFire.Value, Is.False);
            _transport.Results.OnNext(Result("rejected"));
            Assert.That(_session.Recovery.pending, Is.Null);
        }

        [Test]
        public void TimerNotifiesOnlyWhenDisplayedSecondChanges()
        {
            var values = new List<int?>();
            using var subscription = _model.SecondsLeft.Subscribe(values.Add);
            _time.NowMs = 100;
            _model.Tick();
            _time.NowMs = 900;
            _model.Tick();
            _time.NowMs = 1000;
            _model.Tick();
            CollectionAssert.AreEqual(new int?[] { 15, 14 }, values);
            var waiting = Snapshot(2, "a");
            waiting.phase = "waiting";
            _transport.States.OnNext(waiting);
            Assert.That(_model.SecondsLeft.Value, Is.Null);
        }

        [Test]
        public void PendingNotifiesForEachShotEvenWhenFeedbackTextIsUnchanged()
        {
            var changes = 0;
            using var subscription = _model.Pending.Subscribe(_ => changes++);
            _model.FireAsync(0, 0).GetAwaiter().GetResult();
            _transport.Results.OnNext(Result("rejected"));
            _model.Feedback.Value = "Выстрел отправлен. Ожидаем ответ…";
            _model.FireAsync(1, 0).GetAwaiter().GetResult();
            Assert.That(changes, Is.EqualTo(4));
            Assert.That(_model.Pending.Value.x, Is.EqualTo(1));
        }

        [Test]
        public void DeadlineAndDisconnectDisableShooting()
        {
            _time.NowMs = 15000;
            _model.Tick();
            Assert.That(_model.CanFire.Value, Is.False);
            Assert.That(_model.SecondsLeft.Value, Is.Zero);
            _time.NowMs = 0;
            _transport.Connection.Value = ConnectionStatus.Failed;
            _model.Tick();
            Assert.That(_model.CanFire.Value, Is.False);
            Assert.That(_model.Status.Value, Is.EqualTo("Соединение потеряно"));
        }

        private CommandResult Result(string status)
        {
            return new CommandResult
            {
                commandId = _transport.LastCommand.commandId,
                turnId = 1,
                revision = 2,
                status = status,
                reason = status == "rejected" ? "out_of_bounds" : null
            };
        }

        private static MatchState Snapshot(long revision, string active, bool shot = false)
        {
            var player = new PlayerState
            {
                playerId = "a",
                ownShips = new ArraySchema<ShipState>(),
                incomingShots = new ArraySchema<ShotState>(),
                outgoingShots = new ArraySchema<ShotState>()
            };
            if (shot)
            {
                player.outgoingShots = new ArraySchema<ShotState>(new List<ShotState>
                {
                    new() { x = 0, y = 0, result = "miss", sunkCells = new ArraySchema<CellState>() }
                });
            }
            var state = new MatchState
            {
                matchId = "TEST0001", phase = "playing", activePlayerId = active,
                revision = revision, turnId = revision, deadlineMs = 15000,
                boardSize = 6, turnDurationSeconds = 15, players = new MapSchema<PlayerState>()
            };
            state.players["a"] = player;
            return state;
        }

        private sealed class FakeTime : IServerTimeService
        {
            public double NowMs { get; set; }
            public void Synchronize(double serverTimeMs, double roundTripMs)
            {
                NowMs = serverTimeMs + roundTripMs / 2;
            }
            public double RemainingSeconds(double deadlineMs)
            {
                return Math.Max(0, (deadlineMs - NowMs) / 1000);
            }
        }

        private sealed class FakeTransport : ITransportService
        {
            public readonly ReactiveProperty<ConnectionStatus> Connection = new(ConnectionStatus.Connected);
            public IReadOnlyReactiveProperty<ConnectionStatus> Status => Connection;
            public bool CanResume => false;
            public UniTask<bool> ResumeAsync(CancellationToken token)
            {
                return UniTask.FromResult(false);
            }
            public void Leave()
            {
            }
            public readonly Subject<CommandResult> Results = new();
            public readonly Subject<MatchState> States = new();
            public readonly Subject<int> Leaves = new();
            private readonly Subject<string> _errors = new();
            public string RoomId => "TEST0001";
            public string SessionId => "a";
            public MatchState CurrentState { get; set; }
            public IObservable<CommandResult> CommandResults => Results;
            public IObservable<string> Errors => _errors;
            public IObservable<MatchState> StateChanged => States;
            public IObservable<int> Disconnected => Leaves;
            public int SendCount { get; private set; }
            public FireCommand LastCommand { get; private set; }
            public UniTask ConnectAsync(string roomName, bool create, CancellationToken token)
            {
                return UniTask.CompletedTask;
            }
            public UniTask SendAsync(FireCommand command, CancellationToken token)
            {
                LastCommand = command;
                SendCount++;
                return UniTask.CompletedTask;
            }
            public void Disconnect()
            {
            }
            public void Dispose()
            {
                Connection.Dispose();
                Results.Dispose();
                States.Dispose();
                Leaves.Dispose();
                _errors.Dispose();
            }
        }
    }
}

