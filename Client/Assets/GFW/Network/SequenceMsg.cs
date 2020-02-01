using System;

namespace GFW
{
    public class SequenceMsg
    {
        public LinkMsg readMsg;
        public LinkMsg writeMsg;

        public SequenceMsg()
        {
            writeMsg = new LinkMsg();
            readMsg = writeMsg;
        }

        public void Add(NetworkInfo msg)
        {
            if (msg != null)
            {
                this.writeMsg.Push(msg, ref this.writeMsg);
            }
        }

        public NetworkInfo Pop()
        {
            return this.readMsg.Read(ref this.readMsg);
        }

        public void Clear()
        {
            for (NetworkInfo msg = this.Pop(); msg != null; msg = this.Pop())
            {
                if (msg.buffer != null)
                {
                    msg.buffer.Close();
                }
            }
            this.writeMsg = new LinkMsg();
            this.readMsg = this.writeMsg;
        }
    }
}
