namespace Game.Scripts.Infrastructure.Core.UI
{
    public abstract class PopupView<TModel> : PopupViewBase where TModel : PopupModel
    {
        public void Init(TModel model)
        {
            SetModel(model);
        }

        protected abstract void SetModel(TModel model);
    }
}
