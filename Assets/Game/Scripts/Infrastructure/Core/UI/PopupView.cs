using UniRx;

namespace Game.Scripts.Infrastructure.Core.UI
{
    public abstract class PopupView<TModel> : PopupViewBase where TModel : PopupModel
    {
        protected readonly CompositeDisposable Subscriptions = new();
        public void Init(TModel model)
        {
            SetModel(model);
        }

        protected abstract void SetModel(TModel model);
        protected override void OnDestroy()
        {
            Subscriptions.Dispose();
            base.OnDestroy();
        }
    }
}
