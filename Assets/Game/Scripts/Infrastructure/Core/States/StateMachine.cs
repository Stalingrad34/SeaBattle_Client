using Cysharp.Threading.Tasks;
using Game.Scripts.Infrastructure.Core.UI;

namespace Game.Scripts.Infrastructure.Core.States
{
    public class StateMachine
    {
        private readonly StateFactory _stateFactory;
        private readonly UIManager _uiManager;
        private IState _currentState;

        public StateMachine(StateFactory stateFactory, UIManager uiManager)
        {
            _stateFactory = stateFactory;
            _uiManager = uiManager;
        }

        public void Enter<TState>() where TState: class, IEnterState
        {
            ExitCurrentState();
            var state = ChangeState<TState>();
            state.Enter();
        }

        public void Enter<TState, TArgs>(TArgs args) where TState: class, IEnterStateArgs<TArgs>
        {
            ExitCurrentState();
            var state = ChangeState<TState>();
            state.Enter(args);
        }

        public async UniTask EnterAsync<TState>() where TState: class, IEnterStateAsync
        {
            await ExitCurrentStateAsync();
            var state = ChangeState<TState>();
            await state.Enter();
        }

        public async UniTask EnterAsync<TState, TArgs>(TArgs args) where TState: class, IEnterStateArgsAsync<TArgs>
        {
            await ExitCurrentStateAsync();
            var state = ChangeState<TState>();
            await state.Enter(args);
        }

        public void Reset()
        {
            ExitCurrentState();
            _currentState = null;
            _uiManager.Clear();
        }

        private TState ChangeState<TState>() where TState : class, IState
        {
            _uiManager.Clear();

            _currentState = _stateFactory.Create<TState>();
            return _currentState as TState;
        }

        private void ExitCurrentState()
        {
            if (_currentState is IExitState exitState)
                exitState.Exit();
        }

        private async UniTask ExitCurrentStateAsync()
        {
            if (_currentState is IExitStateAsync exitStateAsync)
                await exitStateAsync.ExitAsync();

            if (_currentState is IExitState exitState)
                exitState.Exit();
        }
    }
}
