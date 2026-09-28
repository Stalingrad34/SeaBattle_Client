using Cysharp.Threading.Tasks;
using Game.Scripts.Infrastructure.Core.States;
using Game.Scripts.Infrastructure.Core.UI;
using Game.Scripts.Infrastructure.Implementations.States;
using UniRx;
using Zenject;
using Game.Scripts.Infrastructure.Core.Services;

namespace Game.Scripts.Infrastructure.Implementations.UI.Popups.GameOverPopup
{
    public sealed class GameOverPopupModel : PopupModel
    {
        public class Factory : PlaceholderFactory<bool, GameOverPopupModel>
        {
        }

        public readonly ReactiveProperty<bool> Won = new();
        private readonly StateMachine _states;
        private readonly ITransportService _transport;

        public GameOverPopupModel(bool won, UIManager ui, StateMachine states, ITransportService transport) : base(ui)
        {
            Won.Value = won;
            Won.AddTo(Disposables);
            _states = states;
            _transport = transport;
        }

        public void ReturnToMenu()
        {
            _transport.Leave();
            _states.EnterAsync<GameState>().Forget();
        }
    }
}
