using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Game.Scripts.Infrastructure.Core.UI
{
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField]
        private Transform popupRoot;
        private readonly Dictionary<PopupModel, PopupViewBase> _popups = new();
        private DiContainer _container;
        [Inject]
        public void Construct(DiContainer container)
        {
            _container = container;
        }

        public UniTask ShowPopupAsync<TView, TModel>(TModel model, CancellationToken token)
            where TView : PopupView<TModel> where TModel : PopupModel
        {
            token.ThrowIfCancellationRequested();
            if (_popups.ContainsKey(model))
                return UniTask.CompletedTask;
            var prefab = Resources.Load<TView>("Popups/" + typeof(TView).Name);
            if (prefab == null)
                throw new InvalidOperationException("Popup prefab missing: " + typeof(TView).Name);
            var view = _container.InstantiatePrefabForComponent<TView>(prefab, popupRoot);
            view.Init(model);
            _popups.Add(model, view);
            view.Show();
            return UniTask.CompletedTask;
        }

        public void HidePopup(PopupModel model)
        {
            if (!_popups.Remove(model, out var view))
                return;
            view.gameObject.SetActive(false);
            Destroy(view.gameObject);
            model.Dispose();
        }

        private void OnDestroy()
        {
            foreach (var model in _popups.Keys)
                model.Dispose();
            _popups.Clear();
        }
    }
}
