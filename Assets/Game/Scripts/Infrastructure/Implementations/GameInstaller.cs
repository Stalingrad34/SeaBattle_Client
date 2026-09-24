using Game.Scripts.Infrastructure.Core.Configs;
using Game.Scripts.Infrastructure.Core.Network;
using Game.Scripts.Infrastructure.Core.States;
using Game.Scripts.Infrastructure.Core.UI;
using Game.Scripts.Infrastructure.Implementations.Network;
using Game.Scripts.Infrastructure.Implementations.Services;
using Game.Scripts.Infrastructure.Implementations.States;
using Game.Scripts.Infrastructure.Implementations.UI.Popups.RoomPopup;
using UnityEngine;
using Zenject;

namespace Game.Scripts.Infrastructure.Implementations
{
    [CreateAssetMenu(menuName = "SeaBattle/Game Installer")]
    public sealed class GameInstaller : ScriptableObjectInstaller
    {
        [SerializeField]
        private ConnectionConfig connectionConfig;
        [SerializeField]
        private GameConfig gameConfig;
        [SerializeField]
        private UIManager uiManager;
        public override void InstallBindings()
        {
            Container.BindInstance(connectionConfig);
            Container.BindInstance(gameConfig);
            Container.Bind<UIManager>().FromComponentInNewPrefab(uiManager).AsSingle();
            Container.Bind<StateFactory>().AsSingle();
            Container.BindInterfacesAndSelfTo<StateMachine>().AsSingle();
            Container.Bind<LoadingState>().AsSingle();
            Container.Bind<GameState>().AsSingle();
            Container.BindInterfacesAndSelfTo<ColyseusTransportService>().AsSingle();
            Container.BindInterfacesAndSelfTo<SessionService>().AsSingle();
            Container.BindInterfacesAndSelfTo<MatchService>().AsSingle();
            Container.BindInterfacesAndSelfTo<ServerTimeService>().AsSingle();
            Container.BindInterfacesAndSelfTo<NetworkDiagnostics>().AsSingle();
            Container.BindFactory<RoomPopupModel, RoomPopupModel.Factory>();
        }
    }
}
