using System;

namespace GFW
{
    public static class GlobalEventSystem
    {
        private static EventSystem<int, object> sender = new EventSystem<int, object>();

        public static bool Has(int eventType)
        {
            return sender.Has(eventType);
        }

        public static void Bind(int eventType, EventCallback<object> eventHandler)
        {
            sender.Bind(eventType, eventHandler);
        }

        public static void Fire(int eventType, object eventArg = null)
        {
            sender.Fire(eventType, eventArg);
        }

        public static void UnBind(int eventType, EventCallback<object> eventHandler)
        {
            sender.UnBind(eventType, eventHandler);
        }

        public static void UnBindAll()
        {
            sender.UnBindAll();
        }

    }
}
