using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GFW
{
    public delegate void EventCallback<T>(T arg);

    public class EventSystem<TKey, TValue>
    {
        private Dictionary<TKey, EventCallback<TValue>> dict = new Dictionary<TKey, EventCallback<TValue>>();

        public void Bind(TKey eventType, EventCallback<TValue> eventHandler)
        {
            EventCallback<TValue> callbacks;
            if (dict.TryGetValue(eventType, out callbacks))
            {
                dict[eventType] = callbacks + eventHandler;
            }
            else
            {
                dict.Add(eventType, eventHandler);
            }
        }

        public void UnBind(TKey eventType, EventCallback<TValue> eventHandler)
        {
            EventCallback<TValue> callbacks;
            if (dict.TryGetValue(eventType, out callbacks))
            {
                callbacks = (EventCallback<TValue>)EventCallback<TValue>.RemoveAll(callbacks, eventHandler);
                if (callbacks == null)
                {
                    dict.Remove(eventType);
                }
                else
                {
                    dict[eventType] = callbacks;
                }
            }
        }

        public bool Has(TKey eventType)
        {
            return dict.ContainsKey(eventType);
        }

        public void Fire(TKey eventType, TValue eventArg)
        {
            EventCallback<TValue> callbacks;
            if (dict.TryGetValue(eventType, out callbacks))
            {
                callbacks.Invoke(eventArg);
            }
        }

        public void UnBindAll()
        {
            dict.Clear();
        }
    }
}
