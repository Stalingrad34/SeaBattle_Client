using System;
using System.Collections.Generic;
using Game.Scripts.Infrastructure.Core.Services;
using UniRx;

namespace Game.Scripts.Infrastructure.Implementations.Services
{
    public sealed class NetworkDiagnostics : INetworkDiagnostics
    {
        private readonly Subject<string> _entries = new();
        public IObservable<string> Entries => _entries;
        public readonly ReactiveProperty<bool> Enabled = new(true);
        public readonly ReactiveProperty<string> Log = new("");
        private readonly Queue<string> _history = new();

        // Only metadata is recorded; payloads may contain recovery credentials.
        public void Record(string direction, string messageType)
        {
            if (!Enabled.Value)
                return;
            var entry = $"{DateTime.Now:HH:mm:ss} {direction} {messageType}";
            _history.Enqueue(entry);
            while (_history.Count > 14)
                _history.Dequeue();
            Log.Value = string.Join("\n", _history);
            _entries.OnNext(entry);
        }

        public void Clear()
        {
            _history.Clear();
            Log.Value = "";
        }

        public void Dispose()
        {
            _entries.Dispose();
            Enabled.Dispose();
            Log.Dispose();
        }
    }
}
