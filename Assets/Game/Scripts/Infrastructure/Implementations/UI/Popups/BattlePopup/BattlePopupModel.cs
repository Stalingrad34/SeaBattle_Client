using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Infrastructure.Core.Network;
using Game.Scripts.Infrastructure.Core.Services;
using Game.Scripts.Infrastructure.Core.States;
using Game.Scripts.Infrastructure.Core.UI;
using Game.Scripts.Infrastructure.Implementations.States;
using Game.Scripts.Multiplayer.Generated;
using UniRx;
using Zenject;
using Game.Scripts.Infrastructure.Implementations.UI.Popups.GameOverPopup;

namespace Game.Scripts.Infrastructure.Implementations.UI.Popups.BattlePopup
{
    public sealed class BattlePopupModel : PopupModel
    {
        public class Factory : PlaceholderFactory<BattlePopupModel>
        {
        }

        public string PlayerId => _session.PlayerId;
        public string RoomName => _session.MatchId;
        public IReadOnlyReactiveProperty<MatchState> State => _match.State;
        public readonly ReactiveProperty<string> Status = new("");
        public readonly ReactiveProperty<string> Feedback = new("");
        public readonly ReactiveProperty<bool> CanFire = new(false);
        public readonly ReactiveProperty<FireCommand> Pending = new();
        public readonly ReactiveProperty<int?> SecondsLeft = new();
        
        private readonly ITransportService _transport;
        private readonly IMatchService _match;
        private readonly ISessionService _session;
        private readonly IServerTimeService _time;
        private readonly StateMachine _states;
        private readonly double _responseTimeoutMs;
        private readonly CancellationTokenSource _lifetime = new();
        private CommandResult _result;
        private double _sentAtMs;
        private bool _connected = true;
        private bool _timedOut;
        private bool _resultShown;
        private readonly UIManager _ui;
        private readonly GameOverPopupModel.Factory _resultFactory;

        public BattlePopupModel(UIManager ui, ITransportService transport, IMatchService match,
            ISessionService session, IServerTimeService time, StateMachine states, ConnectionConfig config,
            GameOverPopupModel.Factory resultFactory) : base(ui)
        {
            _ui = ui;
            _resultFactory = resultFactory;
            _transport = transport;
            _match = match;
            _session = session;
            _time = time;
            _states = states;
            _responseTimeoutMs = config.TimeoutSeconds * 1000;
            Status.AddTo(Disposables);
            Feedback.AddTo(Disposables);
            CanFire.AddTo(Disposables);
            Pending.AddTo(Disposables);
            SecondsLeft.AddTo(Disposables);
            transport.StateChanged.Subscribe(state => match.Apply(state)).AddTo(Disposables);
            transport.CommandResults.Subscribe(OnResult).AddTo(Disposables);
            transport.Disconnected.Subscribe(_ => ConnectionLost()).AddTo(Disposables);
            transport.Errors.Subscribe(_ => ConnectionLost()).AddTo(Disposables);
            match.State.Subscribe(StateChanged).AddTo(Disposables);
            match.Apply(transport.CurrentState);
            Observable.EveryUpdate().Subscribe(_ => Tick()).AddTo(Disposables);
        }

        public bool CanShootCell(int x, int y)
        {
            var state = State.Value;
            if (!CanFire.Value || state == null || x < 0 || y < 0 || x >= state.boardSize || y >= state.boardSize)
                return false;
            var player = state.players[PlayerId];
            for (var i = 0; i < player.outgoingShots.Count; i++)
            {
                var shot = player.outgoingShots[i];
                if (shot.x == x && shot.y == y)
                    return false;
            }
            return true;
        }

        public async UniTask FireAsync(int x, int y)
        {
            if (!CanShootCell(x, y))
                return;
            CanFire.Value = false;
            Pending.Value = new FireCommand
            {
                commandId = Guid.NewGuid().ToString("N"),
                turnId = State.Value.turnId,
                x = x,
                y = y
            };
            _sentAtMs = _time.NowMs;
            _result = null;
            _timedOut = false;
            Feedback.Value = "Выстрел отправлен. Ожидаем ответ…";
            Refresh();
            try
            {
                await _transport.SendAsync(Pending.Value, _lifetime.Token);
            }
            catch (OperationCanceledException)
            {
                // The popup has been closed; it no longer owns UI or network callbacks.
            }
            catch (Exception)
            {
                if (!_lifetime.IsCancellationRequested)
                    ConnectionLost();
            }
        }

        private void OnResult(CommandResult result)
        {
            if (Pending.Value == null || result.commandId != Pending.Value.commandId)
                return;
            _result = result;
            if (result.status == "rejected")
            {
                Pending.Value = null;
                Feedback.Value = RejectionText(result.reason);
            }
            Refresh();
        }

        private void Refresh()
        {
            var state = State.Value;
            var remainingSeconds = state?.phase == "playing"
                ? Math.Min(state.turnDurationSeconds, _time.RemainingSeconds(state.deadlineMs)) : 0;
            SecondsLeft.Value = state?.phase == "playing" ? (int)Math.Ceiling(remainingSeconds) : null;
            if (Pending.Value != null && _result?.status == "applied" && state != null && state.revision >= _result.revision)
            {
                var shots = state.players[PlayerId].outgoingShots;
                var feedback = "Выстрел принят";
                for (var i = 0; i < shots.Count; i++)
                {
                    var shot = shots[i];
                    if (shot.x != Pending.Value.x || shot.y != Pending.Value.y)
                        continue;
                    feedback = shot.result == "sunk" ? "Корабль потоплен!"
                        : shot.result == "hit" ? "Попадание!" : "Промах";
                    break;
                }
                Pending.Value = null;
                _result = null;
                Feedback.Value = feedback;
            }
            CanFire.Value = _connected && Pending.Value == null && state?.phase == "playing"
                && state.activePlayerId == PlayerId && remainingSeconds > 0;
            Status.Value = !_connected ? "Соединение потеряно"
                : state == null ? "Получаем состояние…"
                : state.phase == "waiting" ? "Ожидаем второго игрока"
                : state.phase == "finished" ? state.winnerId == PlayerId ? "Победа!" : "Поражение"
                : Pending.Value != null ? "Ожидаем ответ сервера"
                : state.activePlayerId == PlayerId ? "Ваш ход" : "Ход соперника";
        }

        public void Tick()
        {
            if (Pending.Value != null && !_timedOut && _time.NowMs - _sentAtMs >= _responseTimeoutMs)
            {
                _timedOut = true;
                Feedback.Value = "Ответ задерживается. Результат выстрела пока неизвестен.";
            }
            Refresh();
        }

        private void ConnectionLost()
        {
            _connected = false;
            Feedback.Value = Pending.Value == null ? "Вернитесь к выбору комнаты."
                : "Соединение потеряно. Результат выстрела пока неизвестен.";
            Refresh();
        }

        public void Leave()
        {
            _states.EnterAsync<GameState>().Forget();
        }

        private void StateChanged(MatchState state)
        {
            Refresh();
            if (state?.phase == "finished")
                ShowResultAsync().Forget();
        }

        private async UniTask ShowResultAsync()
        {
            if (_resultShown)
                return;
            _resultShown = true;
            var won = State.Value.winnerId == PlayerId;
            try
            {
                // Let the battle view finish opening before placing the result above it.
                await UniTask.Yield(_lifetime.Token);
                var result = _resultFactory.Create(won);
                await _ui.ShowPopupAsync<GameOverPopupView, GameOverPopupModel>(result);
            }
            catch (OperationCanceledException)
            {
                // The battle was closed before the result could be shown.
            }
        }

        public override void Dispose()
        {
            _lifetime.Cancel();
            base.Dispose();
            _transport.Disconnect();
            _match.Reset();
        }

        private static string RejectionText(string reason)
        {
            return reason switch
            {
                "wrong_turn" => "Сейчас ход соперника.",
                "turn_expired" => "Время хода истекло.",
                "already_shot" => "В эту клетку уже стреляли.",
                "match_finished" => "Партия завершена.",
                "match_not_started" => "Ожидаем второго игрока.",
                _ => "Сервер отклонил выстрел."
            };
        }
    }
}

