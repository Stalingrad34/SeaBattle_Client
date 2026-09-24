using System;
using Game.Scripts.Infrastructure.Core.Services;
using UniRx;

namespace Game.Scripts.Infrastructure.Implementations.Services
{
    public sealed class NetworkDiagnostics : INetworkDiagnostics
    {
        private readonly Subject<string> _entries = new();
        public IObservable<string> Entries => _entries;

        // Only metadata is recorded; payloads may contain recovery credentials.
        public void Record(string direction, string messageType)
        {
            _entries.OnNext($"Client: {direction} {messageType}");
        }

        public void Dispose()
        {
            _entries.Dispose();
        }
    }
}
