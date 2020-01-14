using System.Reflection;
using UnityEngine.Events;

namespace GFW
{
    public abstract class BusinessModule : BasicModule
    {
        protected string m_name = null;
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
        internal BusinessModule() { }
        internal BusinessModule(string name) { m_name = name; }

        public virtual void Create(object arg = null) { }
        protected virtual void Start(object arg) { }
    }
}
