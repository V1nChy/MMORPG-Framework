using System;
using System.Collections.Generic;
using UnityEngine;

namespace GFW.ManagerSystem
{
    /// <summary>
    /// 事件命令
    /// </summary>
    public class ControllerCommand : ICommand
    {
        public virtual void Execute(IMessage message)
        {
        }
    }

    public sealed class Facade
    {
        private static Facade ms_instance;
        private GameObject m_Entity;
        private IController m_controller;
        private Dictionary<string, IManager> m_ManagerDic = new Dictionary<string, IManager>();
        private List<IManager> m_ManagerList = new List<IManager>(); 

        public IManager this[string name]
        {
            get
            {
                if(m_ManagerDic.TryGetValue(name, out var mgr))
                {
                    return mgr;
                }
                return null;
            }
            private set { }
        }

        GameObject Entity
        {
            get
            {
                if (m_Entity == null)
                {
                    m_Entity = GameObject.Find("GameManager");
                    if(m_Entity == null)
                    {
                        m_Entity = new GameObject("GameManager");
                    }
                    GameObject.DontDestroyOnLoad(m_Entity);
                }
                return m_Entity;
            }
        }

        public static Facade Instance
        {
            get
            {
                if(ms_instance == null)
                {
                    ms_instance = new Facade();
                }
                return ms_instance;
            }
        }

        private Facade()
        {
            m_controller = Controller.Instance;
            m_Entity = Entity;
        }

        public void RegisterCommand(Type commandType)
        {
            m_controller.RegisterCommand(commandType);
        }

        public void RemoveCommand(string commandName)
        {
            m_controller.RemoveCommand(commandName);
        }

        public bool HasCommand(string commandName)
        {
            return m_controller.HasCommand(commandName);
        }

        public void SendMessageCommand(string commandName, object body = null, string type = null)
        {
            m_controller.ExecuteCommand(new Message(commandName, body, type));
        }

        /// <summary>
        /// 添加管理器
        /// </summary>
        public IManager AddManager(Type type)
        {
            IManager mgr = null;
            if (m_ManagerDic.TryGetValue(type.Name, out mgr))
            {
                return mgr;
            }
            this.Log("AddComponent type = " + type.Name);
            Component c = Entity.AddComponent(type);
            mgr = c as IManager;
            m_ManagerDic.Add(type.Name, mgr);
            m_ManagerList.Add(mgr);
            return mgr;
        }

        /// <summary>
        /// 获取系统管理器
        /// </summary>
        public IManager GetManager(string name)
        {
            IManager mgr = null;
            if (m_ManagerDic.TryGetValue(name, out mgr))
            {
                return mgr;
            }
            return mgr;
        }

        /// <summary>
        /// 删除管理器
        /// </summary>
        public void RemoveManager(string typeName)
        {
            if (!m_ManagerDic.ContainsKey(typeName))
            {
                return;
            }
            IManager manager = null;
            m_ManagerDic.TryGetValue(typeName, out manager);
            if (manager == null)
                return;

            Type type = manager.GetType();
            if (type.IsSubclassOf(typeof(MonoBehaviour)))
            {
                GameObject.Destroy((Component)manager);
            }
            m_ManagerDic.Remove(typeName);
            m_ManagerList.Remove(manager);
        }

        public void StartUpManager()
        {
            for(int i = 0;i < m_ManagerList.Count;i++)
            {
                m_ManagerList[i].DoStart();
            }
        }
    }
}
