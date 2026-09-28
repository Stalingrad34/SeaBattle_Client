using Cysharp.Threading.Tasks;
using Game.Scripts.Infrastructure.Core.States;
using Game.Scripts.Infrastructure.Implementations.States;
using Game.Scripts.Infrastructure.Implementations.UI.DebugPanel;
using UnityEngine;
using Zenject;

namespace Game.Scripts.Infrastructure.Core
{
    public sealed class Bootstrapper : MonoBehaviour
    {
        private StateMachine _states;
        private void Awake()
        {
            ProjectContext.Instance.EnsureIsInitialized();
            _states = ProjectContext.Instance.Container.Resolve<StateMachine>();
            var debugPanel = Resources.Load<DebugPanelView>("DebugPanelView");
            ProjectContext.Instance.Container.InstantiatePrefabForComponent<DebugPanelView>(debugPanel, transform);
        }

        private void Start()
        {
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            _states.EnterAsync<LoadingState>().Forget();
        }

        private void OnDestroy()
        {
            _states?.Reset();
        }
    }
}
