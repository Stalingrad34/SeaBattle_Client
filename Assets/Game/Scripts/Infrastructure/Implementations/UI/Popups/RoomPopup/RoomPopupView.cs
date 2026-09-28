using Game.Scripts.Infrastructure.Core.UI;
using Game.Scripts.Infrastructure.Core.Extensions;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace Game.Scripts.Infrastructure.Implementations.UI.Popups.RoomPopup
{
    public sealed class RoomPopupView : PopupView<RoomPopupModel>
    {
        [SerializeField] private InputField joinInput;
        [SerializeField] private InputField createInput;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button createButton;
        [SerializeField] private Text statusText;
        private RoomPopupModel _model;

        protected override void SetModel(RoomPopupModel model)
        {
            _model = model;
            Bind(joinInput, joinButton, model.JoinRoomName, model.Busy);
            Bind(createInput, createButton, model.CreateRoomName, model.Busy);
            model.Status.Subscribe(StatusChanged).AddTo(gameObject);
            joinButton.OnClick(Join).AddTo(gameObject);
            createButton.OnClick(Create).AddTo(gameObject);
        }

        private void StatusChanged(string value)
        {
            statusText.text = value;
        }

        private void Join()
        {
            _model.ConnectAsync(false).Forget();
        }

        private void Create()
        {
            _model.ConnectAsync(true).Forget();
        }

        private void Bind(InputField input, Button button, ReactiveProperty<string> name, ReactiveProperty<bool> busy)
        {
            input.characterLimit = RoomName.Length;
            input.onValidateInput = (_, _, c) => RoomName.IsAllowed(c) ? c : '\0';
            input.OnValueChanged(value =>
            {
                var normalized = RoomName.Normalize(value);
                input.SetTextWithoutNotify(normalized);
                name.Value = normalized;
            }).AddTo(gameObject);
            name.SubscribeToInputField(input).AddTo(gameObject);
            name.CombineLatest(busy, (value, waiting) => !waiting && RoomName.IsValid(value))
                .SubscribeBtnInteractable(button).AddTo(gameObject);
            busy.Subscribe(value => input.interactable = !value).AddTo(gameObject);
        }

        protected override void OnDestroy()
        {
            joinInput.onValidateInput = null;
            createInput.onValidateInput = null;
            base.OnDestroy();
        }
    }
}
