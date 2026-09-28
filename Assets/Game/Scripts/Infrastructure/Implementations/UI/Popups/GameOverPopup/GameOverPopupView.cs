using Game.Scripts.Infrastructure.Core.Extensions;
using Game.Scripts.Infrastructure.Core.UI;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Infrastructure.Implementations.UI.Popups.GameOverPopup
{
    public sealed class GameOverPopupView : PopupView<GameOverPopupModel>
    {
        [SerializeField] private Text title;
        [SerializeField] private Text description;
        [SerializeField] private Button menuButton;

        protected override void SetModel(GameOverPopupModel model)
        {
            model.Won.Subscribe(WonChanged).AddTo(gameObject);
            menuButton.OnClick(model.ReturnToMenu).AddTo(gameObject);
        }

        private void WonChanged(bool isWon)
        {
            title.text = isWon ? "Победа!" : "Поражение";
            title.color = isWon ? new Color(.3f, .85f, .65f) : new Color(1f, .45f, .4f);
            description.text = isWon ? "Вы выиграли эту партию." : "В этой партии победил соперник.";
        }
    }
}
