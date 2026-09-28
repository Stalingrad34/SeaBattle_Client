using Cysharp.Threading.Tasks;
using Game.Scripts.Infrastructure.Core.Extensions;
using Game.Scripts.Infrastructure.Core.Network;
using Game.Scripts.Infrastructure.Core.UI;
using Game.Scripts.Multiplayer.Generated;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Infrastructure.Implementations.UI.Popups.BattlePopup
{
    public sealed class BattlePopupView : PopupView<BattlePopupModel>
    {
        [SerializeField] private BoardView ownBoard;
        [SerializeField] private BoardView enemyBoard;
        [SerializeField] private Text roomText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text feedbackText;
        [SerializeField] private Button leaveButton;
        private BattlePopupModel _model;

        protected override void SetModel(BattlePopupModel model)
        {
            _model = model;
            roomText.text = "Комната: " + model.RoomName;
            model.Status.Subscribe(StatusChanged).AddTo(gameObject);
            model.Feedback.Subscribe(FeedbackChanged).AddTo(gameObject);
            model.State.Subscribe(StateChanged).AddTo(gameObject);
            model.CanFire.Subscribe(CanFireChanged).AddTo(gameObject);
            model.Pending.Subscribe(PendingChanged).AddTo(gameObject);
            model.SecondsLeft.Subscribe(SecondsLeftChanged).AddTo(gameObject);
            leaveButton.OnClick(model.Leave).AddTo(gameObject);
        }

        private void StatusChanged(string status)
        {
            statusText.text = status;
        }

        private void FeedbackChanged(string feedback)
        {
            feedbackText.text = feedback;
        }

        private void StateChanged(MatchState state)
        {
            Render();
        }

        private void CanFireChanged(bool canFire)
        {
            Render();
        }

        private void PendingChanged(FireCommand pending)
        {
            Render();
        }

        private void SecondsLeftChanged(int? seconds)
        {
            timerText.text = seconds.HasValue ? $"До конца хода: {seconds.Value} с" : "—";
        }

        private void CellClicked(int x, int y)
        {
            _model.FireAsync(x, y).Forget();
        }

        private void Render()
        {
            var state = _model.State.Value;
            if (state?.players == null || !state.players.ContainsKey(_model.PlayerId))
                return;
            ownBoard.Init(state.boardSize, null);
            enemyBoard.Init(state.boardSize, CellClicked);
            var player = state.players[_model.PlayerId];
            ownBoard.Render(player, true, _model.CanShootCell, -1, -1);
            var pending = _model.Pending.Value;
            enemyBoard.Render(player, false, _model.CanShootCell, pending?.x ?? -1, pending?.y ?? -1);
        }
    }
}
