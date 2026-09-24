using System;
using System.Threading;
using System.Threading.Tasks;
using Colyseus;
using Cysharp.Threading.Tasks;
using Game.Scripts.Infrastructure.Core.Network;
using UniRx;

namespace Game.Scripts.Infrastructure.Implementations.Network
{
    /// <summary>Stage 1 connectivity probe; no game or reconnection logic yet.</summary>
    public sealed class ConnectionProbeService : IDisposable
    {
        private readonly ConnectionConfig _config;
        private ColyseusClient _client;
        private ColyseusRoom<NoState> _room;
        private bool _disposed;
        public ReactiveProperty<string> Status { get; } = new("Not connected");
        public bool Succeeded { get; private set; }

        public ConnectionProbeService(ConnectionConfig config) => _config = config;

        public async UniTask RunAsync(CancellationToken cancellationToken)
        {
            Status.Value = "Connecting to Colyseus...";
            _client = new ColyseusClient(_config.Endpoint);
            // Keep observing the SDK task after cancellation to close any late successful join.
            var joining = JoinOwnedAsync();
            _room = await joining.AsUniTask().AttachExternalCancellation(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            _room.OnLeave += OnLeave;
            var response = new UniTaskCompletionSource<string>();
            _room.OnMessage<string>("pong", value => response.TrySetResult(value));
            _room.OnMessage<string>("error", value => response.TrySetException(new InvalidOperationException(value)));
            var nonce = Guid.NewGuid().ToString("N");
            Status.Value = "Connected. Sending ping...";
            await _room.Send("ping", nonce).AsUniTask().AttachExternalCancellation(cancellationToken);
            var received = await response.Task.AttachExternalCancellation(cancellationToken);
            if (received != nonce) throw new InvalidOperationException("Unexpected pong payload.");
            Succeeded = true;
            Status.Value = "Connected: ping / pong OK";
        }

        private async Task<ColyseusRoom<NoState>> JoinOwnedAsync()
        {
            var room = await _client.JoinOrCreate(_config.RoomName);
            if (_disposed)
            {
                await room.Leave(false);
                throw new OperationCanceledException();
            }
            _room = room;
            return room;
        }

        private void OnLeave(int code)
        {
            if (!_disposed) Status.Value = $"Disconnected ({code})";
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_room != null)
            {
                _room.OnLeave -= OnLeave;
                CloseAsync(_room).Forget();
                _room = null;
            }
            Status.Dispose();
        }

        private static async UniTaskVoid CloseAsync(ColyseusRoom<NoState> room)
        {
            try { await room.Leave(false); }
            catch (Exception) { /* Teardown must also work after server shutdown. */ }
        }
    }
}

