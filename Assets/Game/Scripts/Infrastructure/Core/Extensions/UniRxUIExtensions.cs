using System;
using UniRx;
using UnityEngine.UI;

namespace Game.Scripts.Infrastructure.Core.Extensions
{
    public static class UniRxUIExtensions
    {
        public static IDisposable SubscribeBtnInteractable(this IObservable<bool> source, Button button)
        {
            return source.SubscribeWithState(button, (value, target) => target.interactable = value);
        }

        public static IDisposable SubscribeToInputField(this IObservable<string> source, InputField input)
        {
            return source.SubscribeWithState(input, (value, target) => target.SetTextWithoutNotify(value));
        }

        public static IDisposable OnValueChanged(this InputField input, Action<string> callback)
        {
            return input.onValueChanged.AsObservable().Subscribe(value => callback?.Invoke(value));
        }
    }
}
