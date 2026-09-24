using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Infrastructure.Core.Configs;
using Game.Scripts.Infrastructure.Core.Network;
using Game.Scripts.Infrastructure.Core.Services;
using Game.Scripts.Infrastructure.Core.States;

namespace Game.Scripts.Infrastructure.Implementations.States
{
    public sealed class LoadingState : IState
    {
        private readonly GameConfig _game;
        private readonly ConnectionConfig _network;
        private readonly List<IInitializableService> _services;
        private readonly StateMachine _states;
        public LoadingState(GameConfig game, ConnectionConfig network, List<IInitializableService> services, StateMachine states)
        {
            _game = game;
            _network = network;
            _services = services;
            _states = states;
        }

        public async UniTask EnterAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            _game.Validate();
            _network.Validate();
            foreach (var service in _services)
                await service.InitAsync(token);
            token.ThrowIfCancellationRequested();
            // Queue the transition; do not await while this state's Enter holds the transition gate.
            _states.EnterAsync<GameState>(CancellationToken.None).Forget(UnityEngine.Debug.LogException);
        }

        public void Exit()
        {
        }
    }
}
