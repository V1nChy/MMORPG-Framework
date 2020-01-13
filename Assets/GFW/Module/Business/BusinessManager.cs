using System;
using System.Collections.Generic;

namespace GFW
{
    public class BusinessManager : ServiceModule<BusinessManager>
    {
        class MessageObject
        {
            public string target;
            public string msg;
            public object[] args;
            public bool isCall;
        }

        private string m_domain;
        private Dictionary<string, BusinessModule> m_mapModules;
        private Dictionary<string, List<MessageObject>> m_mapCacheMessage;
        private Dictionary<string, EventTable> m_mapPreListenEvents;

        public BusinessManager()
        {
            m_mapModules = new Dictionary<string, BusinessModule>();
            m_mapCacheMessage = new Dictionary<string, List<MessageObject>>();
            m_mapPreListenEvents = new Dictionary<string, EventTable>();
        }

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
            CallMethod(name, "Start", arg);
        }

        /// <summary>
        /// 通过类型创建业务模块
        /// </summary>
        public T CreateModule<T>(object args = null) where T : BusinessModule
        {
            return (T)CreateModule(typeof(T).Name, args);
        }

        /// <summary>
        /// 通过反射创建业务模块
        /// </summary>
        public BusinessModule CreateModule(string name, object arg = null)
        {
            if (m_mapModules.ContainsKey(name))
            {
                LogMgr.LogWarning("The Module<{0}> Has Existed!", name);
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
            EventTable eventTable = null;
            if (m_mapPreListenEvents.ContainsKey(name))
            {
                eventTable = m_mapPreListenEvents[name];
                m_mapPreListenEvents.Remove(name);

            }
            module.SetEventTable(eventTable);

            module.Create(arg);

            //处理缓存的消息
            if (m_mapCacheMessage.ContainsKey(name))
            {
                List<MessageObject> messageList = m_mapCacheMessage[name];
                foreach (MessageObject item in messageList)
                {
                    if (item.isCall)
                        module.CallMethod(item.msg, item.args);
                    else
                        module.HandleMessage(item.msg, item.args);
                }
                m_mapCacheMessage.Remove(name);
            }
            return module;
        }

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

        public BusinessModule GetModule(string name)
        {
            if (m_mapModules.ContainsKey(name))
            {
                return m_mapModules[name];
            }
            return null;
        }

        public void ReleaseAll()
        {
            foreach (var @event in m_mapPreListenEvents)
            {
                @event.Value.UnBindAll();
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
        /// 向指定的模块发送消息
        /// </summary>
        public void SendMessage(string target, string msg, params object[] args)
        {
            SendMessage_Internal(false, target, msg, args);
        }

        public void CallMethod(string target, string name, params object[] args)
        {
            SendMessage_Internal(true, target, name, args);
        }

        private void SendMessage_Internal(bool isCall, string target, string msg, object[] args)
        {
            BusinessModule module = GetModule(target);
            if (module != null)
            {
                if (isCall)
                    module.CallMethod(msg, args);
                else
                    module.HandleMessage(msg, args);
            }
            else
            {
                List<MessageObject> list = GetCacheMessageList(target);
                MessageObject obj = new MessageObject();
                obj.target = target;
                obj.msg = msg;
                obj.args = args;
                obj.isCall = isCall;
                list.Add(obj);
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
    }
}
