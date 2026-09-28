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
using Game.Scripts.Infrastructure.Implementations.UI.Popups.GameOverPopup;
using Game.Scripts.Infrastructure.Implementations.UI.Popups.BattlePopup;
using Game.Scripts.Infrastructure.Implementations.UI.DebugPanel;

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
            Container.Bind<StateMachine>().AsSingle();
            Container.Bind<LoadingState>().AsSingle();
            Container.Bind<GameState>().AsSingle();
            Container.BindInterfacesAndSelfTo<ColyseusTransportService>().AsSingle();
            Container.BindInterfacesAndSelfTo<SessionService>().AsSingle();
            Container.Bind<Game.Scripts.Infrastructure.Core.Services.ISessionStorage>().To<PlayerPrefsSessionStorage>().AsSingle();
            Container.BindInterfacesAndSelfTo<MatchService>().AsSingle();
            Container.BindInterfacesAndSelfTo<ServerTimeService>().AsSingle();
            Container.BindInterfacesAndSelfTo<NetworkDiagnostics>().AsSingle();
            Container.BindInterfacesAndSelfTo<NetworkSimulator>().AsSingle();
            Container.BindInterfacesAndSelfTo<DebugPanelModel>().AsSingle();
            Container.BindFactory<RoomPopupModel, RoomPopupModel.Factory>();
            Container.BindFactory<BattlePopupModel, BattlePopupModel.Factory>();
            Container.BindFactory<bool, GameOverPopupModel, GameOverPopupModel.Factory>();
        }
    }
}
