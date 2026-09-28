using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Game.Scripts.Infrastructure.Core.Network;
using Game.Scripts.Infrastructure.Core.Services;
using UniRx;

namespace Game.Scripts.Infrastructure.Implementations.Network
{
    public sealed class NetworkSimulator : IDisposable
    {
        private sealed class Delivery
        {
            public double Due;
            public string Label;
            public Action Receive;
            public Func<bool> IsCurrent;
            public CancellationToken Token;
        }

        public readonly ReactiveProperty<bool> Blocked = new(false);
        public int Latency { get; private set; }
        public int Jitter { get; private set; }
        public int LossPercent { get; private set; }
        public int DuplicatePercent { get; private set; }
        public int Seed { get; private set; }
        private readonly List<Delivery> _queue = new();
        private readonly INetworkDiagnostics _diagnostics;
        private readonly Func<double> _now;
        private readonly IDisposable _update;
        private Random _random;
        private bool _disposed;

        public NetworkSimulator(ConnectionConfig config, INetworkDiagnostics diagnostics, Func<double> now = null)
        {
            _diagnostics = diagnostics;
            _now = now ?? (() => Stopwatch.GetTimestamp() * 1000.0 / Stopwatch.Frequency);
            Configure(config.LatencyMilliseconds, config.JitterMilliseconds,
                (int)Math.Round(config.LossProbability * 100), (int)Math.Round(config.DuplicateProbability * 100), config.NetworkSeed);
            _update = Observable.EveryUpdate().Subscribe(_ => Tick());
        }

        public void Configure(int latency, int jitter, int loss, int duplicate, int seed)
        {
            if (latency < 0 || latency > 10000 || jitter < 0 || jitter > 10000 || loss < 0 || loss > 100 || duplicate < 0 || duplicate > 100)
                throw new ArgumentException("Задержка и разброс: 0–10000 мс. Потери и дубли: 0–100%.");
            Latency = latency;
            Jitter = jitter;
            LossPercent = loss;
            DuplicatePercent = duplicate;
            Seed = seed;
            _random = new Random(seed);
        }

        public void Deliver(string direction, string message, Action receive, Func<bool> isCurrent, CancellationToken token)
        {
            if (_disposed || token.IsCancellationRequested || !isCurrent())
                return;
            var label = direction + " " + message;
            _diagnostics.Record("попытка", label);
            if (Blocked.Value || _random.Next(100) < LossPercent || _queue.Count >= 4096)
            {
                _diagnostics.Record("потеря", label);
                return;
            }
            Enqueue(label, receive, isCurrent, token);
            if (_random.Next(100) < DuplicatePercent)
            {
                _diagnostics.Record("дубликат", label);
                Enqueue(label, receive, isCurrent, token);
            }
        }

        private void Enqueue(string label, Action receive, Func<bool> isCurrent, CancellationToken token)
        {
            var delay = Math.Max(0, Latency + _random.Next(-Jitter, Jitter + 1));
            _queue.Add(new Delivery { Due = _now() + delay, Label = label, Receive = receive, IsCurrent = isCurrent, Token = token });
        }

        public void Tick()
        {
            if (_disposed)
                return;
            var now = _now();
            // Remove before invoking: a callback may enqueue another message or dispose the client.
            for (var i = 0; i < _queue.Count; i++)
            {
                if (_disposed)
                    return;
                var item = _queue[i];
                if (!item.Token.IsCancellationRequested && item.IsCurrent() && item.Due > now)
                    continue;
                _queue.RemoveAt(i);
                i--;
                if (item.Token.IsCancellationRequested || !item.IsCurrent())
                    _diagnostics.Record("устарело", item.Label);
                else if (Blocked.Value)
                    _diagnostics.Record("потеря", item.Label);
                else
                {
                    _diagnostics.Record("доставка", item.Label);
                    item.Receive();
                }
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _update.Dispose();
            _queue.Clear();
            Blocked.Dispose();
        }
    }
}
