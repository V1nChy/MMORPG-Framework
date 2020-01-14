using System.Reflection;
using UnityEngine.Events;

namespace GFW
{
    public abstract class BasicModule : Module
    {
        protected EventTable m_tblEvent;
        public override void Release()
        {
            base.Release();
            if (m_tblEvent != null)
            {
                m_tblEvent.UnBindAll();
                m_tblEvent = null;
            }
        }

        internal void CallMethod(string method, object[] args)
        {
            //方法名，绑定条件
            MethodInfo mi = this.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            if (mi != null)
            {
                mi.Invoke(this, BindingFlags.NonPublic, null, args, null);
            }
            else
            {
                OnMessage(new Message(method, args[0]));
            }
        }
        internal void HandleMessage(string msg, object[] args)
        {
            OnMessage(new Message(msg, args[0]));
        }
        protected virtual void OnMessage(IMessage msg)
        {

        }

        internal void SetEventTable(EventTable eventTable)
        {
            if (eventTable != null)
            {
                m_tblEvent = eventTable;
            }
            else
            {
                m_tblEvent = new EventTable();
            }
        }
        public void Fire(int eventType, object eventArg = null)
        {
            m_tblEvent.Fire(eventType, eventArg);
        }
        public void Bind(int eventType, EventCallback<object> eventHandler)
        {
            m_tblEvent.Bind(eventType, eventHandler);
        }
        public void UnBind(int eventType, EventCallback<object> eventHandler)
        {
            m_tblEvent.UnBind(eventType, eventHandler);
        }
    }
}
