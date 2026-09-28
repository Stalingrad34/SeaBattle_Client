using System.Collections.Generic;
using System.Threading;
using Colyseus.Schema;
using Game.Scripts.Infrastructure.Core.Network;
using Game.Scripts.Infrastructure.Implementations.Network;
using Game.Scripts.Infrastructure.Implementations.Services;
using Game.Scripts.Multiplayer.Generated;
using NUnit.Framework;
using UniRx;
using UnityEngine;

namespace Game.Tests
{
    public sealed class NetworkSimulatorTests
    {
        private ConnectionConfig _config;
        private NetworkDiagnostics _diagnostics;
        private NetworkSimulator _network;
        private double _now;

        [SetUp]
        public void SetUp()
        {
            _now = 0;
            _config = ScriptableObject.CreateInstance<ConnectionConfig>();
            _diagnostics = new NetworkDiagnostics();
            _network = new NetworkSimulator(_config, _diagnostics, () => _now);
        }

        [TearDown]
        public void TearDown()
        {
            _network.Dispose();
            _diagnostics.Dispose();
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void DelayedMessageWaitsAndSilentBreakDropsAlreadyQueuedMessage()
        {
            var deliveries = 0;
            _network.Configure(200, 0, 0, 0, 1);
            _network.Deliver("out", "fire", () => deliveries++, () => true, CancellationToken.None);
            _now = 199;
            _network.Tick();
            Assert.That(deliveries, Is.Zero);
            _network.Blocked.Value = true;
            _now = 200;
            _network.Tick();
            _network.Blocked.Value = false;
            _network.Tick();
            Assert.That(deliveries, Is.Zero);
            Assert.That(_diagnostics.Log.Value, Does.Contain("потеря"));
        }

        [Test]
        public void CompleteLossAndDuplicationApplyInBothDirections()
        {
            var deliveries = 0;
            foreach (var direction in new[] { "out", "in" })
            {
                _network.Configure(0, 0, 100, 100, 1);
                _network.Deliver(direction, "fire", () => deliveries++, () => true, CancellationToken.None);
                _network.Tick();
                _network.Configure(0, 0, 0, 100, 1);
                _network.Deliver(direction, "fire", () => deliveries++, () => true, CancellationToken.None);
                _network.Tick();
            }
            Assert.That(deliveries, Is.EqualTo(4));
        }

        [Test]
        public void OldConnectionAndCancelledLifetimeNeverReceiveDelayedCallbacks()
        {
            var current = true;
            var deliveries = 0;
            using var lifetime = new CancellationTokenSource();
            _network.Deliver("in", "schema", () => deliveries++, () => current, CancellationToken.None);
            _network.Deliver("in", "result", () => deliveries++, () => true, lifetime.Token);
            current = false;
            lifetime.Cancel();
            _network.Tick();
            Assert.That(deliveries, Is.Zero);
        }

        [Test]
        public void SameSeedAndTrafficProduceSameDeliveredSequence()
        {
            List<int> Run()
            {
                _network.Configure(100, 100, 30, 50, 42);
                var delivered = new List<int>();
                for (var i = 0; i < 40; i++)
                {
                    var id = i;
                    _network.Deliver("out", "fire", () => delivered.Add(id), () => true, CancellationToken.None);
                }
                for (var i = 0; i < 30; i++)
                {
                    _now += 10;
                    _network.Tick();
                }
                return delivered;
            }
            var first = Run();
            Assert.That(first.Count, Is.GreaterThan(0));
            CollectionAssert.AreEqual(first, Run());
        }

        [Test]
        public void SnapshotKeepsNestedShipAndShotDataWhenSdkStateMutates()
        {
            var cell = new CellState { x = 1, y = 2 };
            var ship = new ShipState { id = "ship", cells = new ArraySchema<CellState>(new List<CellState> { cell }) };
            var shot = new ShotState { x = 3, y = 4, result = "sunk", sunkCells = new ArraySchema<CellState>(new List<CellState> { cell }) };
            var original = new MatchState { revision = 1, players = new MapSchema<PlayerState>() };
            original.players["self"] = new PlayerState
            {
                playerId = "self", ownShips = new ArraySchema<ShipState>(new List<ShipState> { ship }),
                outgoingShots = new ArraySchema<ShotState>(new List<ShotState> { shot })
            };
            var copy = MatchSnapshot.Copy(original);
            original.revision = 2;
            cell.x = 9;
            shot.result = "miss";
            Assert.That(copy.revision, Is.EqualTo(1));
            Assert.That(copy.players["self"].ownShips[0].cells[0].x, Is.EqualTo(1));
            Assert.That(copy.players["self"].outgoingShots[0].sunkCells[0].x, Is.EqualTo(1));
            Assert.That(copy.players["self"].outgoingShots[0].result, Is.EqualTo("sunk"));
            Assert.That(copy.players.Count, Is.EqualTo(1));
        }

        [Test]
        public void DisabledLogStopsRecordingAndHistoryIsBounded()
        {
            var notifications = 0;
            using var subscription = _diagnostics.Entries.Subscribe(_ => notifications++);
            _diagnostics.Enabled.Value = false;
            _diagnostics.Record("send", "fire");
            Assert.That(notifications, Is.Zero);
            _diagnostics.Enabled.Value = true;
            for (var i = 0; i < 50; i++)
                _diagnostics.Record("send", "fire");
            Assert.That(_diagnostics.Log.Value.Split('\n').Length, Is.EqualTo(14));
            _diagnostics.Clear();
            Assert.That(_diagnostics.Log.Value, Is.Empty);
        }
    }
}
