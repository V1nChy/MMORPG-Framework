using System;
using System.Collections.Generic;

namespace GFW
{
    public class ModuleManager : ServiceModule<ModuleManager>
    {
        class MessageObject
        {
            public string target;
            public string msg;
            public object[] args;
        }

        #region - 字段定义
        /// <summary>
        /// 业务模块所在的域
        /// </summary>
        private string m_domain;

        /// <summary>
        /// 已创建的模块列表
        /// </summary>
        private Dictionary<string, BusinessModule> m_mapModules;

        /// <summary>
        /// 当目标模块未创建时，缓存的消息对象
        /// </summary>
        private Dictionary<string, List<MessageObject>> m_mapCacheMessage;

        /// <summary>
        /// 当目标模块未创建时，预监听的事件
        /// </summary>
        private Dictionary<string, EventTable> m_mapPreListenEvents;
        #endregion

        public ModuleManager()
        {
            m_mapModules = new Dictionary<string, BusinessModule>();
            m_mapCacheMessage = new Dictionary<string, List<MessageObject>>();
            m_mapPreListenEvents = new Dictionary<string, EventTable>();
        }

        /// <summary>
        /// 初始化业务模块所在的域
        /// </summary>
        /// <param name="domain">业务模块所在的域</param>
        public void Init(string domain = "")
        {
            m_domain = domain;
        }

        /// <summary>
        /// 显示业务模块的默认UI
        /// </summary>
        /// <param name="name"></param>
        public void StartModule(string name, object arg = null)
        {
            SendMessage(name, "Start", arg);
        }

        //========================================================================

        #region 创建和销毁业务模块
        /// <summary>
        /// 通过类型创建一个业务模块
        /// 业务模块必须已经定义
        /// </summary>
        public T CreateModule<T>(object args = null) where T : BusinessModule
        {
            return (T)CreateModule(typeof(T).Name, args);
        }

        /// <summary>
        /// 通过类名创建一个业务模块
        /// 先通过名字反射出Class，如果不存在
        /// </summary>
        /// <param name="name">业务模块(类名)的名字</param>
        public BusinessModule CreateModule(string name, object arg = null)
        {
            LogMgr.Log("Module Name = {0}",name);

            if (m_mapModules.ContainsKey(name))
            {
                LogMgr.LogError("The Module<{0}> Has Existed!", name);
                return null;
            }

            BusinessModule module = null;
            Type type = Type.GetType(m_domain + "." + name);
            if (type != null)
            {
                module = Activator.CreateInstance(type) as BusinessModule;
            }
            else
            {
                module = new LuaModule(name);
                LogMgr.LogWarning("The Module<{0}> Is LuaModule!", name);
            }
            m_mapModules.Add(name, module);

            //处理预监听的事件
            if (m_mapPreListenEvents.ContainsKey(name))
            {
                EventTable eventTable = null;
                eventTable = m_mapPreListenEvents[name];
                m_mapPreListenEvents.Remove(name);

                module.SetEventTable(eventTable);
            }

            module.Create(arg);

            //处理缓存的消息
            if (m_mapCacheMessage.ContainsKey(name))
            {
                List<MessageObject> messageList = m_mapCacheMessage[name];
                foreach (MessageObject item in messageList)
                {
                    module.HandleMessage(item.msg, item.args);
                }
                m_mapCacheMessage.Remove(name);
            }
            return module;
        }

        /// <summary>
        /// 释放一个由ModuleManager创建的模块
        /// 遵守谁创建谁释放的原则
        /// </summary>
        public void ReleaseModule(BusinessModule module)
        {
            if (module != null)
            {
                if (m_mapModules.ContainsKey(module.Name))
                {
                    LogMgr.Log("ReleaseModule name = {0}",module.Name);
                    m_mapModules.Remove(module.Name);
                    module.Release();
                }
                else
                {
                    LogMgr.LogError("模块不是由ModuleManager创建的！ name = {0}", module.Name);
                }
            }
            else
            {
                LogMgr.LogError("module = null!");
            }
        }

        /// <summary>
        /// 释放所有模块
        /// </summary>
        public void ReleaseAll()
        {
            foreach (var @event in m_mapPreListenEvents)
            {
                @event.Value.Clear();
            }
            m_mapPreListenEvents.Clear();

            m_mapCacheMessage.Clear();

            foreach (var module in m_mapModules)
            {
                module.Value.Release();
            }
            m_mapModules.Clear();
        }

        /// <summary>
        /// 通过名字获取一个Module
        /// 如果未创建过该Module，则返回null
        /// </summary>
        /// <param name="name">类名</param>
        public BusinessModule GetModule(string name)
        {
            if (m_mapModules.ContainsKey(name))
            {
                return m_mapModules[name];
            }
            return null;
        }
        #endregion

        //========================================================================

        #region 发送消息给指定模块
        /// <summary>
        /// 向指定的模块发送消息
        /// </summary>
        /// <param name="targetName">模块名</param>
        /// <param name="handlerName">处理函数</param>
        /// <param name="args">参数数组</param>
        public void SendMessage(string targetName, string handlerName, params object[] args)
        {
            SendMessage_Internal(targetName, handlerName, args);
        }

        private void SendMessage_Internal(string target, string msg, object[] args)
        {
            BusinessModule module = GetModule(target);
            if (module != null)
            {
                module.HandleMessage(msg, args);
            }
            else
            {
                List<MessageObject> list = GetCacheMessageList(target);
                MessageObject obj = new MessageObject();
                obj.target = target;
                obj.msg = msg;
                obj.args = args;
                list.Add(obj);

                LogMgr.LogWarning("模块不存在！将消息缓存起来! target:{0}, msg:{1}, args:{2}", target, msg, args);
            }
        }

        private List<MessageObject> GetCacheMessageList(string target)
        {
            List<MessageObject> list = null;
            if (!m_mapCacheMessage.ContainsKey(target))
            {
                list = new List<MessageObject>();
                m_mapCacheMessage.Add(target, list);
            }
            else
            {
                list = m_mapCacheMessage[target];
            }
            return list;
        }
        #endregion

        //========================================================================

        #region
        /// <summary>
        /// 监听指定模块的事件
        /// </summary>
        /// <param name="target">目标模块</param>
        /// <param name="type">事件名</param>
        /// <returns></returns>
        public ModuleEvent Event(string target, string type)
        {
            ModuleEvent evt = null;
            BusinessModule module = GetModule(target);
            if (module != null)
            {
                evt = module.Event(type);
            }
            else
            {
                //预创建事件
                EventTable table = GetPreEventTable(target);
                evt = table.GetEvent(type);
                LogMgr.LogWarning("Event() target不存在！将预监听事件! target:{0}, event:{1}", target, type);
            }
            return evt;
        }

        private EventTable GetPreEventTable(string target)
        {
            EventTable table = null;
            if (!m_mapPreListenEvents.ContainsKey(target))
            {
                table = new EventTable();
                m_mapPreListenEvents.Add(target, table);
            }
            else
            {
                table = m_mapPreListenEvents[target];
            }
            return table;
        }
        #endregion
    }
}
