using System;
using UniRx;

namespace Game.Scripts.Infrastructure.Core.UI
{
    public abstract class PopupModel : IDisposable
    {
        protected readonly CompositeDisposable Disposables = new();
        private readonly UIManager _ui;
        protected PopupModel(UIManager ui)
        {
            _ui = ui;
        }

        public void Close()
        {
            _ui.HidePopup(this);
        }

        public virtual void Dispose()
        {
            Disposables.Dispose();
        }
    }
}
