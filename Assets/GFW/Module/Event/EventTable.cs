using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace GFW
{
    public class ModuleEvent : UnityEvent<object>
    { 
    
    }

    public class ModuleEvent<T> : UnityEvent<T>
    { 
    
    }

    public class EventTable
    {
        private Dictionary<string, ModuleEvent> m_mapEvents;

        public ModuleEvent GetEvent(string eventName)
        { 
            if(m_mapEvents == null)
            {
                m_mapEvents = new Dictionary<string, ModuleEvent>();
            }
            if(!m_mapEvents.ContainsKey(eventName))
            {
                m_mapEvents.Add(eventName, new ModuleEvent());
            }
            return m_mapEvents[eventName];
        }

        public void Clear()
        {
            if(m_mapEvents != null)
            {
                foreach (var item in m_mapEvents)
                {
                    item.Value.RemoveAllListeners();
                }
                m_mapEvents.Clear();
            }
        }
    }
}
