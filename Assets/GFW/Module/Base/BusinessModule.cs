using System.Reflection;

namespace GFW
{
    public abstract class BusinessModule : Module
    {
        private string m_name = null;

        private EventTable m_tblEvent;

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

        public string Title;

        #region - 构造和析构
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
        #endregion

        #region - 事件表
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
        #endregion

        #region - 消息处理
        /// <summary>
        /// 当模块收到消息后，通过反射找到处理函数并执行
        /// </summary>
        /// <param name="method">处理函数名</param>
        /// <param name="args">参数数组</param>
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

        /// <summary>
        /// 消息通用处理函数
        /// 由派生类去实现，用于处理消息
        /// </summary>
        protected virtual void OnModuleMessage(string method, object[] args)
        {
            LogMgr.Log("general hander： method:{0}, args:{1}", method, args);
        }
        #endregion

        //========================================================================

        /// <summary>
        /// 调用它以创建模块
        /// </summary>
        /// <param name="args"></param>
        public virtual void Create(object arg = null)
        {
            LogMgr.Log("{0} create: arg = {1}", Name, arg);
        }

        /// <summary>
        /// 调用它以启动模块
        /// </summary>
        /// <param name="arg"></param>
        protected virtual void Start(object arg)
        {
            LogMgr.Log("业务模块 {0} 显示: arg = {1}", Name, arg);
        }
    }
}
