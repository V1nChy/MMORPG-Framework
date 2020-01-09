using GFW.ManagerSystem;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GFW
{
    public sealed class GameSystem : ServiceModule<GameSystem>
    {
        private string m_domain;
        private MonoBehaviour m_MonoEntity;
        private IController m_controller;
        private Dictionary<string, ManagerModule> m_ManagerDic = new Dictionary<string, ManagerModule>();
        private List<ManagerModule> m_ManagerList = new List<ManagerModule>();

        public GameSystem()
        {
            m_controller = Controller.Instance;
        }

        public ManagerModule this[string name]
        {
            get
            {
                ManagerModule mgr;
                if (m_ManagerDic.TryGetValue(name, out mgr))
                {
                    return mgr;
                }
                return null;
            }
            private set { }
        }

        public void Init(string domain = "")
        {
            this.m_domain = domain;
        }

        public void OnAwake(MonoBehaviour mono)
        {
            m_MonoEntity = mono;
            for (int i = 0;i< m_ManagerList.Count;i++)
            {
                m_ManagerList[i].Awake(mono);
            }
        }

        public void OnStart()
        {
            for (int i = 0; i < m_ManagerList.Count; i++)
            {
                m_ManagerList[i].Start();
            }
        }

        public void OnUpdate()
        {
            for (int i = 0; i < m_ManagerList.Count; i++)
            {
                m_ManagerList[i].Update();
            }
        }

        public void ReleaseAll()
        {
            for (int i = 0; i < m_ManagerList.Count; i++)
            {
                m_ManagerList[i].Release();
            }
            m_ManagerList.Clear();
            m_ManagerList = null;

            m_ManagerDic.Clear();
            m_ManagerDic = null;
        }

        #region - Command
        public void ExecuteCommand(string commandName, object body = null, string type = null)
        {
            if(!this.HasCommand(commandName))
            {
                RegisterCommand(commandName);
            }
            m_controller.ExecuteCommand(new Message(commandName, body, type));
        }

        public void ExecuteCommandOnce(string commandName, object body = null, string type = null)
        {
            this.ExecuteCommand(commandName, body, type);
            this.RemoveCommand(commandName);
        }

        public void RegisterCommand(string commandName)
        {
            Type type = Type.GetType(m_domain + "." + commandName);
            if(type == null)
            {
                this.LogError("Register Command<{0}> Type Not Exist!", commandName);
                return;
            }

            m_controller.RegisterCommand(type);
        }

        public void RemoveCommand(string commandName)
        {
            m_controller.RemoveCommand(commandName);
        }

        public bool HasCommand(string commandName)
        {
            return m_controller.HasCommand(commandName);
        }
        #endregion

        public ManagerModule CreateManager(string mgrName)
        {
            if (this.GetManager(mgrName) != null)
            {
                this.LogError("The Manager<{0}> Has Existed!", mgrName);
                return null;
            }

            Type type = Type.GetType(m_domain + "." + mgrName);
            if (type == null)
            {
                this.LogError("The Manager<{0}> Type Is Error!", mgrName);
                return null;
            }

            return AddManager(type);
        }

        private ManagerModule AddManager(Type type)
        {
            ManagerModule mgr = null;
            if (m_ManagerDic.TryGetValue(type.Name, out mgr))
            {
                return mgr;
            }
            this.Log("AddManager type = " + type.Name);
            mgr = Activator.CreateInstance(type) as ManagerModule;
            m_ManagerDic.Add(type.Name, mgr);
            m_ManagerList.Add(mgr);
            return mgr;
        }

        public ManagerModule GetManager(string name)
        {
            ManagerModule mgr = null;
            if (m_ManagerDic.TryGetValue(name, out mgr))
            {
                return mgr;
            }
            return mgr;
        }

        public T GetManager<T>() where T:ManagerModule
        {
            Type t = typeof(T);
            T mgr = (T)this.GetManager(t.Name);
            if (mgr != null)
            {
                return mgr;
            }
            return default(T);
        }

        public void SendMessage(string mgrName, string handlerName, params object[] args)
        {
            this.SendMessage_Internal(mgrName, handlerName, args);
        }

        private void SendMessage_Internal(string mgrName, string handlerName, object[] args)
        {
            ManagerModule module = GetManager(mgrName);
            if (module != null)
            {
                module.HandleMessage(handlerName, args);
            }
            else
            {
                //List<MessageObject> list = GetCacheMessageList(target);
                //MessageObject obj = new MessageObject();
                //obj.target = target;
                //obj.msg = msg;
                //obj.args = args;
                //list.Add(obj);

                //this.LogWarning("模块不存在！将消息缓存起来! target:{0}, msg:{1}, args:{2}", target, msg, args);
            }
        }
    }
}
