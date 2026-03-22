using System;
using System.Collections.Generic;

namespace Madbox.LiveOps.CloudCode.Signal
{
    public sealed class SignalModule
    {
        private readonly Dictionary<Type, Action<object>> _subscribers = new Dictionary<Type, Action<object>>();

        public void Push<T>(T signal)
        {
            Type type = typeof(T);
            if (_subscribers.TryGetValue(type, out Action<object> handler))
            {
                handler?.Invoke(signal);
            }
        }

        public void Subscribe<T>(Action<T> onNext)
        {
            Type type = typeof(T);

            Action<object> wrappedAction = obj =>
            {
                if (obj is T typedObj)
                {
                    onNext(typedObj);
                }
            };

            if (_subscribers.ContainsKey(type))
            {
                _subscribers[type] += wrappedAction;
            }
            else
            {
                _subscribers[type] = wrappedAction;
            }
        }
    }
}
