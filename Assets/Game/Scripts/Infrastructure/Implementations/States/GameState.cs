using Cysharp.Threading.Tasks;
using Game.Scripts.Infrastructure.Core.States;
using Game.Scripts.Infrastructure.Core.UI;
using Game.Scripts.Infrastructure.Implementations.UI.Popups.RoomPopup;

namespace Game.Scripts.Infrastructure.Implementations.States
{
    public sealed class GameState : IEnterStateAsync
    {
        private readonly UIManager _ui;
        private readonly RoomPopupModel.Factory _factory;

        public GameState(UIManager ui, RoomPopupModel.Factory factory)
        {
            _ui = ui;
            _factory = factory;
        }

        public async UniTask Enter()
        {
            var model = _factory.Create();
            await _ui.ShowPopupAsync<RoomPopupView, RoomPopupModel>(model);
        }
    }
}
