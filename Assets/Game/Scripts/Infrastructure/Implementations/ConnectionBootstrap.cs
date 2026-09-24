using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Infrastructure.Core.Network;
using Game.Scripts.Infrastructure.Implementations.Network;
using PrimeTween;
using UniRx;
using UnityEngine;
using Zenject;

namespace Game.Scripts.Infrastructure.Implementations
{
    public sealed class ConnectionBootstrap : MonoBehaviour
    {
        [SerializeField] private ConnectionConfig config;
        private ServerAvailabilityService _server;
        private ConnectionProbeService _probe;
        private readonly CompositeDisposable _subscriptions = new();
        private CancellationTokenSource _lifetime;
        private Tween _fade;
        private float _opacity;
        private string _status = "Starting...";
        public bool Succeeded => _probe != null && _probe.Succeeded;
        public string Status => _status;

        private void Start()
        {
            if (config == null)
            {
                _status = "Connection config is missing.";
                Debug.LogError(_status);
                return;
            }
            // Minimal composition root for stage 1. Full installers/states follow in stage 2.
            var container = new DiContainer();
            container.BindInstance(config);
            container.Bind<ServerAvailabilityService>().AsSingle();
            container.Bind<ConnectionProbeService>().AsSingle();
            _server = container.Resolve<ServerAvailabilityService>();
            _probe = container.Resolve<ConnectionProbeService>();
            _probe.Status.Subscribe(value => _status = value).AddTo(_subscriptions);
            _lifetime = new CancellationTokenSource();
            _fade = Tween.Custom(0f, 1f, .3f, value => _opacity = value);
            RunAsync(_lifetime.Token).Forget();
        }

        private async UniTaskVoid RunAsync(CancellationToken lifetime)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime);
            timeout.CancelAfterSlim(TimeSpan.FromSeconds(config.TimeoutSeconds));
            try
            {
                _status = "Waiting for server...";
                await _server.EnsureReadyAsync(timeout.Token);
                await _probe.RunAsync(timeout.Token);
                Debug.Log("[SeaBattle] Unity -> Colyseus -> Unity: ping/pong OK");
            }
            catch (OperationCanceledException)
            {
                if (!lifetime.IsCancellationRequested)
                {
                    _status = "Connection timed out. Check server and endpoint.";
                    _probe.Dispose();

                }
            }
            catch (Exception exception)
            {
                if (lifetime.IsCancellationRequested) return;
                _status = exception.Message;
                Debug.LogWarning($"[SeaBattle] {_status}");
                _probe.Dispose();

            }
        }

        private void OnGUI()
        {
            GUI.color = new Color(1, 1, 1, _opacity);
            GUILayout.BeginArea(new Rect(30, 30, Mathf.Min(700, Screen.width - 60), 190), GUI.skin.box);
            GUILayout.Label("SeaBattle - connection check");
            GUILayout.Space(12);
            GUILayout.Label(_status);
            GUILayout.Label(config != null ? config.Endpoint : "No config");
            GUILayout.Space(12);
            GUILayout.Label("Stage 1: dependencies, scene and Colyseus connection.");
            GUILayout.EndArea();
            GUI.color = Color.white;
        }

        private void OnDestroy()
        {
            _lifetime?.Cancel();
            _lifetime?.Dispose();
            _subscriptions.Dispose();
            _probe?.Dispose();

            _fade.Stop();
        }
    }
}

