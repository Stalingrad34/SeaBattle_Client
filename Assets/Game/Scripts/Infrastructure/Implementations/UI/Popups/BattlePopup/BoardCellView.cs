using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Infrastructure.Implementations.UI.Popups.BattlePopup
{
    public sealed class BoardCellView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Text label;

        public void Init(int x, int y, Action<int, int> clicked)
        {
            name = $"Cell_{x}_{y}";
            button.onClick.AddListener(() => clicked?.Invoke(x, y));
        }

        public void Render(string text, Color color, bool interactable)
        {
            label.text = text;
            background.color = color;
            button.interactable = interactable;
        }

        public void SetInteractable(bool value)
        {
            button.interactable = value;
        }
    }
}
