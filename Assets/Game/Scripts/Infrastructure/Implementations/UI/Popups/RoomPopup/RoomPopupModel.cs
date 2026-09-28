using Game.Scripts.Infrastructure.Core.UI;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Infrastructure.Core.Services;
using Game.Scripts.Infrastructure.Implementations.UI.Popups.BattlePopup;
using UniRx;
using Zenject;

namespace Game.Scripts.Infrastructure.Implementations.UI.Popups.RoomPopup
{
    public sealed class RoomPopupModel : PopupModel
    {
        public class Factory : PlaceholderFactory<RoomPopupModel>
        {
        }

        public readonly ReactiveProperty<string> JoinRoomName = new("");
        public readonly ReactiveProperty<string> CreateRoomName = new("");
        public readonly ReactiveProperty<bool> Busy = new(false);
        public readonly ReactiveProperty<string> Status = new("");
        private readonly ITransportService _transport;
        private readonly IMatchService _match;
        private readonly UIManager _ui;
        private readonly BattlePopupModel.Factory _battleFactory;
        private readonly CancellationTokenSource _lifetime = new();
        private bool _enteredBattle;

        public RoomPopupModel(UIManager ui, ITransportService transport, IMatchService match, BattlePopupModel.Factory battleFactory) : base(ui)
        {
            _ui = ui;
            _transport = transport;
            _match = match;
            _battleFactory = battleFactory;
            JoinRoomName.AddTo(Disposables);
            CreateRoomName.AddTo(Disposables);
            Busy.AddTo(Disposables);
            Status.AddTo(Disposables);
        }

        public async UniTask ConnectAsync(bool create)
        {
            var name = create ? CreateRoomName.Value : JoinRoomName.Value;
            if (Busy.Value || !RoomName.IsValid(name))
                return;
            Busy.Value = true;
            Status.Value = "Подключение…";
            try
            {
                _match.Reset();
                await _transport.ConnectAsync(name, create, _lifetime.Token);
                await EnterBattleAsync();
            }
            catch (OperationCanceledException)
            {
                if (!_lifetime.IsCancellationRequested)
                    Status.Value = "Сервер не ответил. Проверьте соединение и повторите.";
            }
            catch (Exception error)
            {
                if (!_lifetime.IsCancellationRequested)
                    Status.Value = error.Message.Contains("room_name_taken")
                        ? "Это имя уже занято. Введите другое."
                        : error.Message.Contains("invalid_game_config")
                        ? "Сервер отклонил параметры игры. Проверьте GameConfig."
                        : create ? "Не удалось создать комнату. Проверьте сервер."
                        : "Комната не найдена, заполнена или уже начала игру.";
                _transport.Disconnect();
            }
            finally
            {
                if (!_lifetime.IsCancellationRequested)
                    Busy.Value = false;
            }
        }

        public async UniTask ResumeAsync()
        {
            if (!_transport.CanResume || Busy.Value)
                return;
            Busy.Value = true;
            Status.Value = "Восстанавливаем предыдущую партию…";
            try
            {
                _match.Reset();
                if (await _transport.ResumeAsync(_lifetime.Token))
                {
                    await EnterBattleAsync();
                }
                else
                {
                    Status.Value = _transport.Status.Value == Game.Scripts.Infrastructure.Core.Network.ConnectionStatus.SessionExpired
                        ? "Партия больше недоступна. Создайте новую комнату."
                        : "Сессию восстановить не удалось. Можно начать новую партию.";
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
                if (!_lifetime.IsCancellationRequested)
                    Status.Value = "Не удалось восстановить соединение.";
            }
            finally
            {
                if (!_lifetime.IsCancellationRequested)
                    Busy.Value = false;
            }
        }

        private async UniTask EnterBattleAsync()
        {
            _lifetime.Token.ThrowIfCancellationRequested();
            var battle = _battleFactory.Create();
            await _ui.ShowPopupAsync<BattlePopupView, BattlePopupModel>(battle);
            _enteredBattle = true;
            Close();
        }

        public override void Dispose()
        {
            _lifetime.Cancel();
            if (!_enteredBattle)
                _transport.Disconnect();
            base.Dispose();
        }
    }
}
