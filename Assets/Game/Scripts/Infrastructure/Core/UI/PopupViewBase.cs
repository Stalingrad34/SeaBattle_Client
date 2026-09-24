using PrimeTween;
using UnityEngine;

namespace Game.Scripts.Infrastructure.Core.UI
{
    public abstract class PopupViewBase : MonoBehaviour
    {
        [SerializeField]
        private CanvasGroup canvasGroup;
        private Tween _fade;
        public void Show()
        {
            canvasGroup.alpha = 0;
            _fade = Tween.Alpha(canvasGroup, 1, .2f);
        }

        protected virtual void OnDestroy()
        {
            _fade.Stop();
        }
    }
}
