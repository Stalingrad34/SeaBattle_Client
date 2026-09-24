using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Infrastructure.Core.Configs;
using Game.Scripts.Infrastructure.Core.Network;
using Game.Scripts.Infrastructure.Core.Services;
using Game.Scripts.Infrastructure.Core.States;

namespace Game.Scripts.Infrastructure.Implementations.States
{
    public sealed class LoadingState : IEnterStateAsync
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

        public async UniTask Enter()
        {
            _game.Validate();
            _network.Validate();
            foreach (var service in _services)
                await service.InitAsync(CancellationToken.None);
            
            _states.EnterAsync<GameState>().Forget();
        }

    }
}
