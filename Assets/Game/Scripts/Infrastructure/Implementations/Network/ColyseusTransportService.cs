using System;
using System.Threading;
using System.Threading.Tasks;
using Colyseus;
using Cysharp.Threading.Tasks;
using Game.Scripts.Infrastructure.Core.Network;
using Game.Scripts.Infrastructure.Core.Services;
using UniRx;
using UnityEngine;
using Game.Scripts.Multiplayer.Generated;

namespace Game.Scripts.Infrastructure.Implementations.Network
{
    public sealed class ColyseusTransportService : ITransportService, IInitializableService
    {
        private readonly ConnectionConfig _config;
        private readonly INetworkDiagnostics _diagnostics;
        private readonly ISessionService _session;
        private readonly Subject<MatchState> _stateChanged = new();
        private readonly UniTaskCompletionSource _firstState = new();
        private readonly Subject<string> _messages = new();
        private readonly Subject<int> _disconnected = new();
        private ColyseusClient _client;
        private ColyseusRoom<MatchState> _room;
        private UniTaskCompletionSource<string> _pong;
        private bool _disposed;
        private bool _connecting;
        private bool _used;
        public bool IsInitialized => _client != null;
        public string RoomId => _room?.RoomId;
        public string SessionId => _room?.SessionId;
        public IObservable<string> Messages => _messages;
        public IObservable<MatchState> StateChanged => _stateChanged;
        public IObservable<int> Disconnected => _disconnected;

        public ColyseusTransportService(ConnectionConfig config, INetworkDiagnostics diagnostics, ISessionService session)
        {
            _config = config;
            _diagnostics = diagnostics;
            _session = session;
        }

        public UniTask InitAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (_disposed)
                throw new ObjectDisposedException(nameof(ColyseusTransportService));
            _config.Validate();
            _client ??= new ColyseusClient(_config.Endpoint);
            return UniTask.CompletedTask;
        }

        public async UniTask ConnectAsync(CancellationToken token)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ColyseusTransportService));
            if (_used)
                throw new InvalidOperationException("Transport already used.");
            token.ThrowIfCancellationRequested();
            await InitAsync(token);
            _used = true;
            _connecting = true;
            try
            {
                await JoinOwnedAsync(token).AsUniTask().AttachExternalCancellation(token);
                await _firstState.Task.AttachExternalCancellation(token);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private async Task JoinOwnedAsync(CancellationToken token)
        {
            try
            {
                // Every application independently requests a seat from server matchmaking.
                var room = await _client.JoinOrCreate<MatchState>(_config.RoomName);
                if (_disposed || token.IsCancellationRequested)
                {
                    await room.Leave(false);
                    throw new OperationCanceledException(token);
                }

                _room = room;
                _session.BeginConnection(room.RoomId, room.SessionId);
                room.OnStateChange += OnStateChange;
                if (room.State?.players != null && room.State.players.ContainsKey(room.SessionId))
                    OnStateChange(room.State, true);
                room.OnLeave += OnLeave;
                room.OnError += OnError;
                room.OnMessage<string>(Protocol.ServerChannel, json =>
                {
                    if (_disposed)
                        return;
                    _diagnostics.Record("receive", Protocol.ServerChannel);
                    _messages.OnNext(json);
                });
                room.OnMessage<string>("pong", value =>
                {
                    if (!_disposed)
                        _pong?.TrySetResult(value);
                });
                room.OnMessage<string>("error", value =>
                {
                    if (!_disposed)
                        _pong?.TrySetException(new InvalidOperationException(value));
                });
                _diagnostics.Record("connected", room.RoomId);
            }
            finally
            {
                _connecting = false;
                if (_disposed)
                    ReleaseClient();
            }
        }

        public async UniTask ProbeAsync(CancellationToken token)
        {
            EnsureConnected();
            if (_pong != null)
                throw new InvalidOperationException("Probe already in progress.");
            var response = _pong = new UniTaskCompletionSource<string>();
            try
            {
                var nonce = Guid.NewGuid().ToString("N");
                await _room.Send("ping", nonce).AsUniTask().AttachExternalCancellation(token);
                if (await response.Task.AttachExternalCancellation(token) != nonce)
                    throw new InvalidOperationException("Unexpected pong.");
                _diagnostics.Record("receive", "pong");
            }
            finally
            {
                _pong = null;
            }
        }

        public async UniTask SendAsync(ClientMessage message, CancellationToken token)
        {
            EnsureConnected();
            token.ThrowIfCancellationRequested();
            var json = Protocol.Encode(message);
            _diagnostics.Record("send", message.type);
            await _room.Send(Protocol.ClientChannel, json).AsUniTask().AttachExternalCancellation(token);
        }

        private void EnsureConnected()
        {
            if (_disposed || _room == null || !_room.Connection.IsOpen)
                throw new InvalidOperationException("Transport is not connected.");
        }

        private void OnStateChange(MatchState state, bool isFirstState)
        {
            if (_disposed)
                return;
            _diagnostics.Record("receive", "schema");
            _stateChanged.OnNext(state);
            if (state.matchId == RoomId && state.players != null && state.players.ContainsKey(SessionId))
                _firstState.TrySetResult();
        }

        private void OnLeave(int code)
        {
            if (_disposed)
                return;
            _pong?.TrySetException(new InvalidOperationException($"Connection closed ({code})."));
            _disconnected.OnNext(code);
        }

        private void OnError(int code, string message)
        {
            if (_disposed)
                return;
            _pong?.TrySetException(new InvalidOperationException($"Transport error ({code})."));
            _disconnected.OnNext(code);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _firstState.TrySetCanceled();
            _pong?.TrySetCanceled();
            if (_room != null)
            {
                _room.OnLeave -= OnLeave;
                _room.OnStateChange -= OnStateChange;
                _room.OnError -= OnError;
                CloseAsync(_room).Forget();
                _room = null;
            }

            _messages.Dispose();
            _stateChanged.Dispose();
            _disconnected.Dispose();
            if (!_connecting)
                ReleaseClient();
        }

        private void ReleaseClient()
        {
            if (_client == null)
                return;
            // SDK creates a transient ScriptableObject when constructed from a URL.
            if (_client.Settings != null)
                UnityEngine.Object.Destroy(_client.Settings);
            _client = null;
        }

        private static async UniTaskVoid CloseAsync(ColyseusRoom<MatchState> room)
        {
            try
            {
                await room.Leave(false);
            }
            catch (Exception)
            { /* Socket may already be gone during teardown. */
            }
        }
    }
}
