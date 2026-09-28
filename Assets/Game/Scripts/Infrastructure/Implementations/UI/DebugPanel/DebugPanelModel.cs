using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Infrastructure.Core.Network;
using Game.Scripts.Infrastructure.Core.Services;
using Game.Scripts.Infrastructure.Core.States;
using Game.Scripts.Infrastructure.Implementations.Network;
using Game.Scripts.Infrastructure.Implementations.Services;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Game.Scripts.Infrastructure.Implementations.UI.DebugPanel
{
    public sealed class DebugPanelModel : IDisposable
    {
        public readonly ReactiveProperty<bool> IsOpen = new(false);
        public readonly ReactiveProperty<bool> Busy = new(false);
        public readonly ReactiveProperty<string> Status = new("");
        public readonly ReactiveProperty<string> Feedback = new("");
        public IReadOnlyReactiveProperty<string> Log => _diagnostics.Log;
        public IReadOnlyReactiveProperty<bool> Logging => _diagnostics.Enabled;
        public NetworkSimulator Network { get; }
        private readonly NetworkDiagnostics _diagnostics;
        private readonly ITransportService _transport;
        private readonly ISessionService _session;
        private readonly StateMachine _states;
        private readonly CompositeDisposable _subscriptions = new();
        private readonly CancellationTokenSource _lifetime = new();

        public DebugPanelModel(NetworkSimulator network, NetworkDiagnostics diagnostics,
            ITransportService transport, ISessionService session, StateMachine states)
        {
            Network = network;
            _diagnostics = diagnostics;
            _transport = transport;
            _session = session;
            _states = states;
            transport.Status.Subscribe(_ => UpdateStatus()).AddTo(_subscriptions);
            network.Blocked.Subscribe(_ => UpdateStatus()).AddTo(_subscriptions);
        }

        public void TogglePanel()
        {
            IsOpen.Value = !IsOpen.Value;
        }

        public void Apply(string latency, string jitter, string loss, string duplicate, string seed)
        {
            if (!int.TryParse(latency, out var l) || !int.TryParse(jitter, out var j) ||
                !int.TryParse(loss, out var p) || !int.TryParse(duplicate, out var d) || !int.TryParse(seed, out var s))
            {
                Feedback.Value = "Введите целые числа во все поля.";
                return;
            }
            try
            {
                Network.Configure(l, j, p, d, s);
                Feedback.Value = "Применено к новым сообщениям в обе стороны.";
            }
            catch (ArgumentException error)
            {
                Feedback.Value = error.Message;
            }
        }

        public void BreakConnection()
        {
            Network.Blocked.Value = true;
            Feedback.Value = "Сообщения теряются. Ждём обнаружения обрыва.";
        }

        public void Connect()
        {
            Network.Blocked.Value = false;
            if (_transport.Status.Value == ConnectionStatus.SessionExpired)
            {
                Feedback.Value = "Партия больше недоступна. Вернитесь в меню.";
                return;
            }
            Feedback.Value = "Доставка разрешена с текущими настройками сети.";
            if (_transport.CanResume && _transport.Status.Value == ConnectionStatus.Failed)
                ResumeAsync().Forget();
        }

        private async UniTaskVoid ResumeAsync()
        {
            try
            {
                await _transport.ResumeAsync(_lifetime.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
                if (!_lifetime.IsCancellationRequested)
                    Feedback.Value = "Восстановить соединение не удалось.";
            }
        }

        public void SetLogging(bool enabled)
        {
            _diagnostics.Enabled.Value = enabled;
        }

        public void ClearLog()
        {
            _diagnostics.Clear();
        }

        public void RecreateClient()
        {
            ReloadAsync(true).Forget();
        }

        public void RestartScene()
        {
            ReloadAsync(false).Forget();
        }

        private async UniTaskVoid ReloadAsync(bool recreateClient)
        {
            if (Busy.Value)
                return;
            Busy.Value = true;
            var scene = SceneManager.GetActiveScene().path;
            try
            {
                _states.Reset();
                _transport.Disconnect();
                if (recreateClient)
                {
                    // Destroy the container and services; only persisted recovery data survives.
                    UnityEngine.Object.Destroy(ProjectContext.Instance.gameObject);
                    await UniTask.NextFrame();
                }
                await SceneManager.LoadSceneAsync(scene);
            }
            catch (Exception error)
            {
                if (!_lifetime.IsCancellationRequested)
                    Feedback.Value = error.Message;
            }
            finally
            {
                if (!_lifetime.IsCancellationRequested)
                    Busy.Value = false;
            }
        }

        private void UpdateStatus()
        {
            var state = _transport.Status.Value switch
            {
                ConnectionStatus.Connected => "Подключён",
                ConnectionStatus.Connecting => "Подключение…",
                ConnectionStatus.Reconnecting => "Восстановление…",
                ConnectionStatus.Failed => "Связь потеряна",
                ConnectionStatus.SessionExpired => "Партия больше недоступна",
                _ => "Вне комнаты"
            };
            Status.Value = state + (Network.Blocked.Value ? " • доставка выключена" : "")
                + (string.IsNullOrEmpty(_session.MatchId) ? "" : "\nКомната: " + _session.MatchId);
        }

        public void Dispose()
        {
            _lifetime.Cancel();
            _subscriptions.Dispose();
            IsOpen.Dispose();
            Busy.Dispose();
            Status.Dispose();
            Feedback.Dispose();
        }
    }
}
