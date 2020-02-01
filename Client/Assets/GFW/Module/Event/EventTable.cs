using System;

namespace GFW
{
    public class EventTable
    {
        private EventSystem<int, object> sender = new EventSystem<int, object>();

        public bool Has(int eventType)
        {
            return sender.Has(eventType);
        }

        public void Bind(int eventType, EventCallback<object> eventHandler)
        {
            sender.Bind(eventType, eventHandler);
        }

        public void Fire(int eventType, object eventArg = null)
        {
            sender.Fire(eventType, eventArg);
        }

        public void UnBind(int eventType, EventCallback<object> eventHandler)
        {
            sender.UnBind(eventType, eventHandler);
        }

        public void UnBindAll()
        {
            sender.UnBindAll();
        }
    }
}
