using GFW.ManagerSystem;
using System;

namespace GFW
{
    public sealed class GameSystem : ServiceModule<GameSystem>
    {
        private string m_domain;
        private Facade m_Facade;

        public GameSystem()
        {
            m_Facade = Facade.Instance;
        }

        public void Init(string domain = "")
        {
            this.m_domain = domain;
        }

        public void StartUp()
        {
            m_Facade.StartUpManager();
        }

        public void ExecuteCommand(string commandName, object body = null, string type = null)
        {
            if(!m_Facade.HasCommand(commandName))
            {
                RegisterCommand(commandName);
            }
            m_Facade.SendMessageCommand(commandName, body, type);
        }

        public void ExecuteCommandOnce(string commandName, object body = null, string type = null)
        {
            this.ExecuteCommand(commandName, body, type);
            m_Facade.RemoveCommand(commandName);
        }

        public void RegisterCommand(string commandName)
        {
            Type type = Type.GetType(m_domain + "." + commandName);
            if(type == null)
            {
                this.LogError("Register Command<{0}> Type Not Exist!", commandName);
                return;
            }

            m_Facade.RegisterCommand(type);
        }

        public void RemoveCommand(string commandName)
        {
            m_Facade.RemoveCommand(commandName);
        }

        public IManager CreateManager(string name)
        {
            if (m_Facade[name] != null)
            {
                this.LogError("The Manager<{0}> Has Existed!", name);
                return null;
            }

            Type type = Type.GetType(m_domain + "." + name);
            if (type == null)
            {
                this.LogError("The Manager<{0}> Type Is Error!", name);
                return null;
            }

            return m_Facade.AddManager(type);
        }

        public IManager GetManager(string name)
        {
            return m_Facade.GetManager(name);
        }

        public T GetManager<T>()
        {
            Type t = typeof(T);
            IManager mgr = m_Facade.GetManager(t.Name);
            if (mgr != null)
            {
                return (T)mgr;
            }
            return default(T);
        }
    }
}
