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

        public ReactiveProperty<string> JoinRoomName { get; } = new("");
        public ReactiveProperty<string> CreateRoomName { get; } = new("");
        public ReactiveProperty<bool> Busy { get; } = new(false);
        public ReactiveProperty<string> Status { get; } = new("");
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
                _lifetime.Token.ThrowIfCancellationRequested();
                var battle = _battleFactory.Create();
                await _ui.ShowPopupAsync<BattlePopupView, BattlePopupModel>(battle);
                _enteredBattle = true;
                Close();
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

        public override void Dispose()
        {
            _lifetime.Cancel();
            if (!_enteredBattle)
                _transport.Disconnect();
            base.Dispose();
        }
    }
}
