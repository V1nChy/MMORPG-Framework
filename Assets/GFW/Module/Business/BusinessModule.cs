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
                m_tblEvent.Clear();
                m_tblEvent = null;
            }
        }

        protected EventTable GetEventTable()
        {
            if (m_tblEvent == null)
            {
                m_tblEvent = new EventTable();
            }
            return m_tblEvent;
        }
        internal void SetEventTable(EventTable eventTable)
        {
            m_tblEvent = eventTable;
        }
        public ModuleEvent Event(string eventName)
        {
            return GetEventTable().GetEvent(eventName);
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
