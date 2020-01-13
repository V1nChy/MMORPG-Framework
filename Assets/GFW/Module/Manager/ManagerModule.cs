using System.Collections;
using System.Reflection;
using UnityEngine;

namespace GFW
{
    public abstract class ManagerModule : Module
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

        protected MonoBehaviour m_Mono;
        private EventTable m_tblEvent;

        public ManagerModule()
        {
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

        public void Bind(int eventType, EventCallback<object> eventHandler)
        {
            m_tblEvent.Bind(eventType, eventHandler);
        }

        public void Fire(int eventType, object eventArg = null)
        {
            m_tblEvent.Fire(eventType, eventArg);
        }

        internal void HandleMessage(string method, object[] args)
        {
            LogMgr.Log("method:{0}, args:{1}", method, args);

            //反射机制
            //类型定义，方法定义
            //方法名，绑定条件
            MethodInfo mi = this.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            if (mi != null)
            {
                mi.Invoke(this, BindingFlags.NonPublic, null, args, null);
            }
            else
            {
                OnModuleMessage(method, args);
            }
        }

        protected virtual void OnModuleMessage(string method, object[] args)
        {
            LogMgr.Log("general hander： method:{0}, args:{1}", method, args);
        }

        /////管理器生命周期
        public virtual void Awake(MonoBehaviour mono)
        {
            m_Mono = mono;
        }

        public virtual void Start()
        {
        }

        public virtual void Update()
        {
        }

        public virtual void Release()
        {
            base.Release();
            if (m_tblEvent != null)
            {
                m_tblEvent.UnBindAll();
                m_tblEvent = null;
            }
        }

        public Coroutine StartCoroutine(IEnumerator routine)
        {
            return m_Mono.StartCoroutine(routine);
        }
    }
}