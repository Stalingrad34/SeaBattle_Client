using Game.Scripts.Infrastructure.Core.UI;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Infrastructure.Implementations.UI.Popups.RoomPopup
{
    public sealed class RoomPopupView : PopupView<RoomPopupModel>
    {
        [SerializeField]
        private InputField joinInput;
        [SerializeField]
        private InputField createInput;
        [SerializeField]
        private Button joinButton;
        [SerializeField]
        private Button createButton;
        protected override void SetModel(RoomPopupModel model)
        {
            Bind(joinInput, joinButton, model.JoinRoomName);
            Bind(createInput, createButton, model.CreateRoomName);
        }

        private void Bind(InputField input, Button button, ReactiveProperty<string> name)
        {
            input.characterLimit = RoomName.Length;
            input.onValidateInput = (_, _, c) => RoomName.IsAllowed(c) ? c : '\0';
            input.OnValueChangedAsObservable().Subscribe(value =>
            {
                var normalized = RoomName.Normalize(value);
                input.SetTextWithoutNotify(normalized);
                name.Value = normalized;
            }).AddTo(Subscriptions);
            name.Subscribe(value =>
            {
                input.SetTextWithoutNotify(value);
                button.interactable = RoomName.IsValid(value);
            }).AddTo(Subscriptions);
        }

        protected override void OnDestroy()
        {
            joinInput.onValidateInput = null;
            createInput.onValidateInput = null;
            base.OnDestroy();
        }
    }
}
