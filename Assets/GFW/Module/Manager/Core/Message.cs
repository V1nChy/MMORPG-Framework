using System;

namespace GFW
{
    public class Message : IMessage
    {
        private string m_name;
        private string m_type;
        private object m_body;

        public Message(string name)
            : this(name, null, null)
        { }

        public Message(string name, object body)
            : this(name, body, null)
        { }

        public Message(string name, object body, string type)
        {
            m_name = name;
            m_body = body;
            m_type = type;
        }

        public override string ToString()
        {
            string msg = "Notification Name: " + Name;
            msg += "\nBody:" + ((Body == null) ? "null" : Body.ToString());
            msg += "\nType:" + ((Type == null) ? "null" : Type);
            return msg;
        }

        public virtual string Name
        {
            get { return m_name; }
        }

        public virtual object Body
        {
            get
            {
                // Setting and getting of reference types is atomic, no need to lock here
                return m_body;
            }
            set
            {
                // Setting and getting of reference types is atomic, no need to lock here
                m_body = value;
            }
        }

        public virtual string Type
        {
            get
            {
                return m_type;
            }
            set
            {
                m_type = value;
            }
        }
    }
}

