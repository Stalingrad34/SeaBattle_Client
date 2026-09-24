using Game.Scripts.Infrastructure.Core.UI;
using UniRx;
using Zenject;

namespace Game.Scripts.Infrastructure.Implementations.UI.Popups.RoomPopup
{
    public sealed class RoomPopupModel : PopupModel
    {
        public class Factory : PlaceholderFactory<RoomPopupModel>
        {
        }

        public ReactiveProperty<string> JoinRoomName { get; } = new("");
        public ReactiveProperty<string> CreateRoomName { get; } = new("");

        public RoomPopupModel(UIManager ui) : base(ui)
        {
            JoinRoomName.AddTo(Disposables);
            CreateRoomName.AddTo(Disposables);
        }
    // Room actions will be added in the next step.
    }
}
