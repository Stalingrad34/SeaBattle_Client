using Game.Scripts.Infrastructure.Core.Extensions;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Game.Scripts.Infrastructure.Implementations.UI.DebugPanel
{
    public sealed class DebugPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button toggleButton;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button breakButton;
        [SerializeField] private Button connectButton;
        [SerializeField] private Button recreateButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private InputField latency;
        [SerializeField] private InputField jitter;
        [SerializeField] private InputField loss;
        [SerializeField] private InputField duplicate;
        [SerializeField] private InputField seed;
        [SerializeField] private Toggle logging;
        [SerializeField] private Text status;
        [SerializeField] private Text feedback;
        [SerializeField] private Text log;
        private DebugPanelModel _model;

        [Inject]
        public void Construct(DebugPanelModel model)
        {
            _model = model;
            latency.SetTextWithoutNotify(model.Network.Latency.ToString());
            jitter.SetTextWithoutNotify(model.Network.Jitter.ToString());
            loss.SetTextWithoutNotify(model.Network.LossPercent.ToString());
            duplicate.SetTextWithoutNotify(model.Network.DuplicatePercent.ToString());
            seed.SetTextWithoutNotify(model.Network.Seed.ToString());
            model.IsOpen.Subscribe(OpenChanged).AddTo(gameObject);
            model.Status.Subscribe(StatusChanged).AddTo(gameObject);
            model.Feedback.Subscribe(FeedbackChanged).AddTo(gameObject);
            model.Log.Subscribe(LogChanged).AddTo(gameObject);
            model.Logging.Subscribe(LoggingChanged).AddTo(gameObject);
            model.Busy.Subscribe(BusyChanged).AddTo(gameObject);
            toggleButton.OnClick(model.TogglePanel).AddTo(gameObject);
            applyButton.OnClick(Apply).AddTo(gameObject);
            breakButton.OnClick(model.BreakConnection).AddTo(gameObject);
            connectButton.OnClick(model.Connect).AddTo(gameObject);
            recreateButton.OnClick(model.RecreateClient).AddTo(gameObject);
            restartButton.OnClick(model.RestartScene).AddTo(gameObject);
            clearButton.OnClick(model.ClearLog).AddTo(gameObject);
            logging.OnValueChangedAsObservable().Subscribe(model.SetLogging).AddTo(gameObject);
        }

        private void Apply()
        {
            _model.Apply(latency.text, jitter.text, loss.text, duplicate.text, seed.text);
        }

        private void OpenChanged(bool open)
        {
            panel.SetActive(open);
        }

        private void StatusChanged(string value)
        {
            status.text = value;
        }

        private void FeedbackChanged(string value)
        {
            feedback.text = value;
        }

        private void LogChanged(string value)
        {
            log.text = value;
        }

        private void LoggingChanged(bool value)
        {
            logging.SetIsOnWithoutNotify(value);
        }

        private void BusyChanged(bool busy)
        {
            recreateButton.interactable = !busy;
            restartButton.interactable = !busy;
            breakButton.interactable = !busy;
            connectButton.interactable = !busy;
        }
    }
}
