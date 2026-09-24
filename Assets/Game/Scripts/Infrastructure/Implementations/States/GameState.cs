using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Infrastructure.Core.States;
using Game.Scripts.Infrastructure.Core.UI;
using Game.Scripts.Infrastructure.Implementations.UI.Popups.RoomPopup;

namespace Game.Scripts.Infrastructure.Implementations.States
{
    public sealed class GameState : IState
    {
        private readonly UIManager _ui;
        private readonly RoomPopupModel.Factory _factory;
        private RoomPopupModel _popup;
        public GameState(UIManager ui, RoomPopupModel.Factory factory)
        {
            _ui = ui;
            _factory = factory;
        }

        public UniTask EnterAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            _popup = _factory.Create();
            return _ui.ShowPopupAsync<RoomPopupView, RoomPopupModel>(_popup, token);
        }

        public void Exit()
        {
            if (_popup == null)
                return;
            _ui.HidePopup(_popup);
            _popup = null;
        }
    }
}
