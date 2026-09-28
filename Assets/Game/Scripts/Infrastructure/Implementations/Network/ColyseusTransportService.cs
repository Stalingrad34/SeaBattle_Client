using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Colyseus;
using Cysharp.Threading.Tasks;
using Game.Scripts.Infrastructure.Core.Configs;
using Game.Scripts.Infrastructure.Core.Network;
using Game.Scripts.Infrastructure.Core.Services;
using Game.Scripts.Multiplayer.Generated;
using UniRx;

namespace Game.Scripts.Infrastructure.Implementations.Network
{
    public sealed class ColyseusTransportService : ITransportService, IInitializableService
    {
        private readonly ConnectionConfig _config;
        private readonly GameConfig _game;
        private readonly INetworkDiagnostics _diagnostics;
        private readonly ISessionService _session;
        private readonly IServerTimeService _time;
        private readonly NetworkSimulator _network;
        private MatchState _currentState;
        private readonly Subject<MatchState> _stateChanged = new();
        private readonly Subject<CommandResult> _commandResults = new();
        private readonly ReactiveProperty<ConnectionStatus> _status = new(ConnectionStatus.Idle);
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private ColyseusClient _client;
        private ColyseusRoom<MatchState> _room;
        private CancellationTokenSource _run;
        private Task<ColyseusRoom<MatchState>> _opening;
        private bool _disposed;
        private bool _recovering;
        private long _generation;
        private long _clockRequest;
        private long _awaitedClockRequest;
        private double _clockSentAt;
        private double _lastClockReply;
        private double _nextHeartbeat;

        public MatchState CurrentState => _currentState;
        public IReadOnlyReactiveProperty<ConnectionStatus> Status => _status;
        public bool CanResume => !string.IsNullOrEmpty(_session.Recovery.reconnectionToken)
            && _session.Recovery.endpoint == _config.Endpoint;
        public IObservable<CommandResult> CommandResults => _commandResults;
        public IObservable<MatchState> StateChanged => _stateChanged;

        public ColyseusTransportService(ConnectionConfig config, INetworkDiagnostics diagnostics,
            ISessionService session, IServerTimeService time, GameConfig game, NetworkSimulator network)
        {
            _config = config;
            _diagnostics = diagnostics;
            _session = session;
            _time = time;
            _game = game;
            _network = network;
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
            await InitAsync(token);
            if (create)
                _game.Validate();
            if (_recovering || _status.Value == ConnectionStatus.Connecting)
                throw new InvalidOperationException("Already connecting.");
            Leave();
            var run = StartRun();
            using var operation = CancellationTokenSource.CreateLinkedTokenSource(token, run);
            _status.Value = ConnectionStatus.Connecting;
            try
            {
                await OpenAsync(roomName, create, false, operation.Token);
                _status.Value = ConnectionStatus.Connected;
                MonitorAsync(run).Forget();
            }
            catch
            {
                CloseRoom(false);
                if (!_disposed && !run.IsCancellationRequested)
                    _status.Value = ConnectionStatus.Failed;
                throw;
            }
        }

        public async UniTask<bool> ResumeAsync(CancellationToken token)
        {
            await InitAsync(token);
            if (!CanResume)
                return false;
            if (_recovering)
            {
                await UniTask.WaitUntil(() => !_recovering, cancellationToken: token);
                return _status.Value == ConnectionStatus.Connected;
            }
            _recovering = true;
            var run = StartRun();
            using var operation = CancellationTokenSource.CreateLinkedTokenSource(token, run);
            _status.Value = ConnectionStatus.Reconnecting;
            var deadline = _clock.Elapsed.TotalSeconds + _game.ReconnectGraceSeconds;
            try
            {
                while (!operation.IsCancellationRequested && _clock.Elapsed.TotalSeconds < deadline)
                {
                    try
                    {
                        await OpenAsync(_session.MatchId, false, true, operation.Token);
                        _status.Value = ConnectionStatus.Connected;
                        MonitorAsync(run).Forget();
                        return true;
                    }
                    catch (MatchMakeException error) when (!operation.IsCancellationRequested
                        && error.Code == ColyseusErrorCode.MATCHMAKE_INVALID_ROOM_ID)
                    {
                        // A disposed room cannot be recovered. Keep the last board/unknown shot,
                        // but invalidate the persisted token so buttons and restarts cannot retry it.
                        CloseRoom(false);
                        _session.SaveConnection(_session.Recovery.endpoint, _session.MatchId, _session.PlayerId, null);
                        _status.Value = ConnectionStatus.SessionExpired;
                        _diagnostics.Record("восстановление", "комната больше недоступна");
                        return false;
                    }
                    catch (Exception) when (!operation.IsCancellationRequested)
                    {
                        CloseRoom(false);
                        // The server may still be detecting the old socket's loss.
                        await UniTask.Delay(TimeSpan.FromSeconds(_config.RetryDelaySeconds),
                            ignoreTimeScale: true, cancellationToken: operation.Token);
                    }
                }
                operation.Token.ThrowIfCancellationRequested();
                _status.Value = ConnectionStatus.Failed;
                return false;
            }
            finally
            {
                _recovering = false;
            }
        }

        private CancellationToken StartRun()
        {
            StopRun();
            CloseRoom(false);
            _run = new CancellationTokenSource();
            return _run.Token;
        }

        private async UniTask OpenAsync(string name, bool create, bool reconnect, CancellationToken token)
        {
            if (_opening != null && !_opening.IsCompleted)
                throw new InvalidOperationException("Previous connection is still finishing.");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            using var timer = timeout.CancelAfterSlim(TimeSpan.FromSeconds(_config.TimeoutSeconds), DelayType.Realtime);
            await UniTask.WaitUntil(() => !_network.Blocked.Value, cancellationToken: timeout.Token);
            var previousToken = _session.Recovery.reconnectionToken;
            _opening = reconnect
                ? _client.Reconnect<MatchState>(new ReconnectionToken { RoomId = name, Token = previousToken })
                : create
                    ? _client.Create<MatchState>(_config.RoomName, new Dictionary<string, object>
                    {
                        ["roomName"] = name,
                        ["gameConfig"] = new Dictionary<string, object>
                        {
                            ["BoardSize"] = _game.BoardSize,
                            ["ShipLengths"] = (int[])_game.ShipLengths.Clone(),
                            ["TurnDurationSeconds"] = _game.TurnDurationSeconds
                        }
                    })
                    : _client.JoinById<MatchState>(name);
            ColyseusRoom<MatchState> room;
            try
            {
                room = await _opening.AsUniTask().AttachExternalCancellation(timeout.Token);
            }
            catch
            {
                CloseLateRoomAsync(_opening, reconnect, previousToken).Forget();
                throw;
            }
            _room = room;
            var generation = ++_generation;
            _session.SaveConnection(_config.Endpoint, room.RoomId, room.SessionId, room.ReconnectionToken.Token);
            var stateReady = new UniTaskCompletionSource();
            var timeReady = new UniTaskCompletionSource();
            var runToken = _run.Token;
            void Receive(string label, Action action)
            {
                _network.Deliver("←", label, action, () => IsCurrent(room, generation), runToken);
            }
            void ReceiveState(MatchState state)
            {
                if (state?.players == null || !state.players.ContainsKey(room.SessionId))
                    return;
                var snapshot = MatchSnapshot.Copy(state);
                Receive("schema r" + snapshot.revision, () =>
                {
                    if (_currentState != null && snapshot.revision <= _currentState.revision)
                        return;
                    _currentState = snapshot;
                    stateReady.TrySetResult();
                    _stateChanged.OnNext(snapshot);
                });
            }
            room.OnStateChange += (state, first) =>
            {
                if (!IsCurrent(room, generation))
                    return;
                ReceiveState(state);
            };
            room.OnMessage<CommandResult>("commandResult", result =>
            {
                if (!IsCurrent(room, generation))
                    return;
                Receive("commandResult " + result.commandId + " " + result.status, () => _commandResults.OnNext(result));
            });
            room.OnMessage<ServerTimeReply>("serverTime", reply =>
            {
                if (!IsCurrent(room, generation))
                    return;
                // Periodic snapshots also repair a lost final patch when no further moves occur.
                ReceiveState(room.State);
                Receive("serverTime " + reply.requestId, () =>
                {
                    if (reply.requestId != _awaitedClockRequest)
                        return;
                    _lastClockReply = _clock.Elapsed.TotalMilliseconds;
                    _time.Synchronize(reply.serverTimeMs, _lastClockReply - _clockSentAt);
                    _awaitedClockRequest = 0;
                    timeReady.TrySetResult();
                });
            });
            room.OnMessage<string>("error", error =>
            {
                if (IsCurrent(room, generation))
                    Receive("error", () => _diagnostics.Record("ошибка сервера", error));
            });
            room.OnLeave += code =>
            {
                if (IsCurrent(room, generation))
                {
                    stateReady.TrySetException(new InvalidOperationException("Connection closed."));
                    timeReady.TrySetException(new InvalidOperationException("Connection closed."));
                    Recover();
                }
            };
            room.OnError += (code, message) =>
            {
                if (IsCurrent(room, generation))
                    Recover();
            };
            ReceiveState(room.State);
            _lastClockReply = _clock.Elapsed.TotalMilliseconds;
            _awaitedClockRequest = 0;
            RequestClock(room, timeout.Token);
            await UniTask.WhenAll(stateReady.Task, timeReady.Task).AttachExternalCancellation(timeout.Token);
            if (!IsCurrent(room, generation))
                throw new OperationCanceledException(token);
            _diagnostics.Record("connected", room.RoomId);
        }

        private bool IsCurrent(ColyseusRoom<MatchState> room, long generation)
        {
            return !_disposed && _room == room && _generation == generation;
        }

        private void RequestClock(ColyseusRoom<MatchState> room, CancellationToken token)
        {
            _awaitedClockRequest = ++_clockRequest;
            _clockSentAt = _clock.Elapsed.TotalMilliseconds;
            _nextHeartbeat = _clockSentAt + _config.HeartbeatIntervalSeconds * 1000;
            var request = _awaitedClockRequest;
            var generation = _generation;
            _network.Deliver("→", "serverTime " + request,
                () => SendWireAsync(room, "serverTime", request, token).Forget(), () => IsCurrent(room, generation), token);
        }

        private async UniTaskVoid MonitorAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && _status.Value == ConnectionStatus.Connected)
                {
                    if (_clock.Elapsed.TotalMilliseconds - _lastClockReply >= _config.SilenceTimeoutSeconds * 1000)
                    {
                        Recover();
                        return;
                    }
                    if (_awaitedClockRequest == 0 && _clock.Elapsed.TotalMilliseconds >= _nextHeartbeat)
                        RequestClock(_room, token);
                    await UniTask.Delay(100, ignoreTimeScale: true, cancellationToken: token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
                if (!token.IsCancellationRequested)
                    Recover();
            }
        }

        private void Recover()
        {
            if (_disposed || _recovering || _status.Value != ConnectionStatus.Connected)
                return;
            ResumeAutomaticallyAsync().Forget();
        }

        private async UniTaskVoid ResumeAutomaticallyAsync()
        {
            try
            {
                await ResumeAsync(CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
                if (!_disposed)
                    _status.Value = ConnectionStatus.Failed;
            }
        }

        public UniTask SendAsync(FireCommand command, CancellationToken token)
        {
            if (_disposed || _status.Value != ConnectionStatus.Connected || _room == null)
                throw new InvalidOperationException("Transport is not connected.");
            var room = _room;
            var generation = _generation;
            var payload = new FireCommand { commandId = command.commandId, turnId = command.turnId, x = command.x, y = command.y };
            _network.Deliver("→", "fire " + command.commandId,
                () => SendWireAsync(room, "fire", payload, token).Forget(), () => IsCurrent(room, generation), token);
            return UniTask.CompletedTask;
        }

        private async UniTaskVoid SendWireAsync(ColyseusRoom<MatchState> room, string type, object payload, CancellationToken token)
        {
            try
            {
                await room.Send(type, payload).AsUniTask().AttachExternalCancellation(token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
                if (!token.IsCancellationRequested && _room == room)
                    Recover();
            }
        }

        public void Disconnect()
        {
            StopRun();
            CloseRoom(false);
            if (!_disposed)
                _status.Value = ConnectionStatus.Idle;
        }

        public void Leave()
        {
            StopRun();
            CloseRoom(true);
            if (!_disposed)
                _status.Value = ConnectionStatus.Idle;
            _session.Clear();
        }

        private void StopRun()
        {
            if (_run == null)
                return;
            _run.Cancel();
            _run.Dispose();
            _run = null;
        }

        private void CloseRoom(bool consented)
        {
            var room = _room;
            _room = null;
            _currentState = null;
            _generation++;
            if (room != null)
                CloseAsync(room, consented).Forget();
        }

        private async UniTaskVoid CloseLateRoomAsync(Task<ColyseusRoom<MatchState>> opening, bool reconnect, string oldToken)
        {
            try
            {
                var room = await opening;
                var preserve = reconnect && _session.Recovery.reconnectionToken == oldToken;
                if (preserve)
                    _session.SaveConnection(_config.Endpoint, room.RoomId, room.SessionId, room.ReconnectionToken.Token);
                await room.Leave(!preserve);
            }
            catch (Exception)
            {
            }
            finally
            {
                if (_disposed)
                    ReleaseClient();
            }
        }

        private static async UniTaskVoid CloseAsync(ColyseusRoom<MatchState> room, bool consented)
        {
            try
            {
                await room.Leave(consented);
            }
            catch (Exception)
            {
                // Closing an already broken connection needs no further action.
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Disconnect();
            _commandResults.Dispose();
            _stateChanged.Dispose();
            _status.Dispose();
            if (_opening == null || _opening.IsCompleted)
                ReleaseClient();
        }

        private void ReleaseClient()
        {
            if (_client == null)
                return;
            if (_client.Settings != null)
                UnityEngine.Object.Destroy(_client.Settings);
            _client = null;
        }
    }
}
