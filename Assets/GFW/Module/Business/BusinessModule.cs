using System.Reflection;
using UnityEngine.Events;

namespace GFW
{
    public abstract class BusinessModule : Module
    {
        private string m_name = null;
        public string Name
        {
            get
            {
                if (m_name == null)
                {
                    m_name = this.GetType().Name;
                }
                return m_name;
            }
        }
        private EventTable m_tblEvent;

        internal BusinessModule() { }

        /// <summary>
        /// 指定模块名，同一程序集调用，派生类调用
        /// </summary>
        internal BusinessModule(string name) { m_name = name; }

        public override void Release()
        {
            base.Release();
            if (m_tblEvent != null)
            {
                m_tblEvent.UnBindAll();
                m_tblEvent = null;
            }
        }

        internal void SetEventTable(EventTable eventTable)
        {
            if(eventTable != null)
            {
                m_tblEvent = eventTable;
            }
            else
            {
                m_tblEvent = new EventTable();
            }
        }

        public void Bind(int eventType, EventCallback<object> eventHandler)
        {
            m_tblEvent.Bind(eventType, eventHandler);
        }

        public void Fire(int eventType, object eventArg = null)
        {
            m_tblEvent.Fire(eventType, eventArg);
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

        public virtual void Create(object arg = null)
        {
        }

        protected virtual void Start(object arg)
        {
        }
    }
}
