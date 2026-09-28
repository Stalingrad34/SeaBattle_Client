using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Colyseus;
using Cysharp.Threading.Tasks;
using Game.Scripts.Infrastructure.Core.Network;
using Game.Scripts.Infrastructure.Core.Services;
using Game.Scripts.Multiplayer.Generated;
using UniRx;

namespace Game.Scripts.Infrastructure.Implementations.Network
{
    public sealed class ColyseusTransportService : ITransportService, IInitializableService
    {
        private readonly ConnectionConfig _config;
        private readonly INetworkDiagnostics _diagnostics;
        private readonly ISessionService _session;
        private readonly IServerTimeService _time;
        private readonly Subject<MatchState> _stateChanged = new();
        private readonly Subject<CommandResult> _commandResults = new();
        private readonly Subject<string> _errors = new();
        private readonly Subject<int> _disconnected = new();
        private ColyseusClient _client;
        private ColyseusRoom<MatchState> _room;
        private bool _disposed;
        private bool _connecting;

        public bool IsInitialized => _client != null;
        public string RoomId => _room?.RoomId;
        public string SessionId => _room?.SessionId;
        public MatchState CurrentState => _room?.State;
        public IObservable<CommandResult> CommandResults => _commandResults;
        public IObservable<string> Errors => _errors;
        public IObservable<MatchState> StateChanged => _stateChanged;
        public IObservable<int> Disconnected => _disconnected;

        public ColyseusTransportService(ConnectionConfig config, INetworkDiagnostics diagnostics, ISessionService session, IServerTimeService time)
        {
            _config = config;
            _diagnostics = diagnostics;
            _session = session;
            _time = time;
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

        public async UniTask ConnectAsync(string roomName, bool create, CancellationToken token)
        {
            if (_room != null || _connecting)
                throw new InvalidOperationException("Already connected or connecting.");
            await InitAsync(token);
            _connecting = true;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            using var timeoutTimer = timeout.CancelAfterSlim(TimeSpan.FromSeconds(_config.TimeoutSeconds));
            try
            {
                await JoinAsync(roomName, create, timeout.Token).AsUniTask().AttachExternalCancellation(timeout.Token);
            }
            catch
            {
                Disconnect();
                throw;
            }
        }

        private async Task JoinAsync(string roomName, bool create, CancellationToken token)
        {
            try
            {
                var room = create
                    ? await _client.Create<MatchState>(_config.RoomName, new Dictionary<string, object> { ["roomName"] = roomName })
                    : await _client.JoinById<MatchState>(roomName);
                // Colyseus join has no cancellation token: close a late connection after scene teardown.
                if (_disposed || token.IsCancellationRequested)
                {
                    await room.Leave(false);
                    throw new OperationCanceledException(token);
                }
                _room = room;
                var stateReady = new UniTaskCompletionSource();
                var clockReady = new UniTaskCompletionSource();
                _session.BeginConnection(room.RoomId, room.SessionId);
                room.OnStateChange += OnStateChange;
                room.OnStateChange += (state, _) =>
                {
                    if (state.players != null && state.players.ContainsKey(room.SessionId))
                        stateReady.TrySetResult();
                };
                room.OnLeave += OnLeave;
                room.OnError += OnError;
                room.OnMessage<CommandResult>("commandResult", result =>
                {
                    if (_disposed)
                        return;
                    _diagnostics.Record("receive", "commandResult");
                    _commandResults.OnNext(result);
                });
                room.OnMessage<string>("error", error =>
                {
                    if (!_disposed)
                        _errors.OnNext(error);
                });
                var clock = Stopwatch.StartNew();
                room.OnMessage<double>("serverTime", serverTime =>
                {
                    if (_disposed || _room != room)
                        return;
                    _time.Synchronize(serverTime, clock.Elapsed.TotalMilliseconds);
                    clockReady.TrySetResult();
                });
                if (room.State?.players != null && room.State.players.ContainsKey(room.SessionId))
                {
                    OnStateChange(room.State, true);
                    stateReady.TrySetResult();
                }
                await room.Send("serverTime");
                await UniTask.WhenAll(stateReady.Task, clockReady.Task).AttachExternalCancellation(token);
                _diagnostics.Record("connected", room.RoomId);
            }
            finally
            {
                _connecting = false;
                if (_disposed)
                    ReleaseClient();
            }
        }

        public async UniTask SendAsync(FireCommand command, CancellationToken token)
        {
            if (_disposed || _room == null || !_room.Connection.IsOpen)
                throw new InvalidOperationException("Transport is not connected.");
            token.ThrowIfCancellationRequested();
            _diagnostics.Record("send", "fire");
            await _room.Send("fire", command).AsUniTask().AttachExternalCancellation(token);
        }

        private void OnStateChange(MatchState state, bool isFirstState)
        {
            if (_disposed)
                return;
            _diagnostics.Record("receive", "schema");
            _stateChanged.OnNext(state);
        }

        private void OnLeave(int code)
        {
            if (!_disposed)
                _disconnected.OnNext(code);
        }

        private void OnError(int code, string message)
        {
            if (!_disposed)
                _errors.OnNext(message);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Disconnect();
            _commandResults.Dispose();
            _errors.Dispose();
            _stateChanged.Dispose();
            _disconnected.Dispose();
            if (!_connecting)
                ReleaseClient();
        }

        public void Disconnect()
        {
            if (_room != null)
            {
                _room.OnLeave -= OnLeave;
                _room.OnError -= OnError;
                _room.OnStateChange -= OnStateChange;
                CloseAsync(_room).Forget();
                _room = null;
            }
        }

        private void ReleaseClient()
        {
            if (_client == null)
                return;
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
            {
                // The connection may already be closed when the scene is unloaded.
            }
        }
    }
}
