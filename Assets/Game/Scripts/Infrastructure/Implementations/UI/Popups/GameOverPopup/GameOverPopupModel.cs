using Cysharp.Threading.Tasks;
using Game.Scripts.Infrastructure.Core.States;
using Game.Scripts.Infrastructure.Core.UI;
using Game.Scripts.Infrastructure.Implementations.States;
using UniRx;
using Zenject;

namespace Game.Scripts.Infrastructure.Implementations.UI.Popups.GameOverPopup
{
    public sealed class GameOverPopupModel : PopupModel
    {
        public class Factory : PlaceholderFactory<bool, GameOverPopupModel>
        {
        }

        public readonly ReactiveProperty<bool> Won = new();
        private readonly StateMachine _states;

        public GameOverPopupModel(bool won, UIManager ui, StateMachine states) : base(ui)
        {
            Won.Value = won;
            _states = states;
        }

        public void ReturnToMenu()
        {
            _states.EnterAsync<GameState>().Forget();
        }
    }
}
