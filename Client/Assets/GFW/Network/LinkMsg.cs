using System;

namespace GFW
{
    public class LinkMsg
    {
        private const byte MaxMsg = 100;
        private static int Listcount = 1;

        public NetworkInfo[] msgList = new NetworkInfo[MaxMsg];
        public LinkMsg next;

        public int count = 0;
        public int pos = 0;

        public void Push(NetworkInfo msg, ref LinkMsg pCurLink)
        {
            msgList[count] = msg;
            count++;
            if (count >= msgList.Length)
            {
                this.next = new LinkMsg();
                pCurLink = this.next;
            }
        }

        public NetworkInfo Read(ref LinkMsg pCurLink)
        {
            NetworkInfo msg = null;
            if (count > pos)
            {
                msg = msgList[pos];
                msgList[pos] = null;
                pos++;
            }
            else if (this.next != null)
            {
                pCurLink = this.next;
                LinkMsg.Listcount++;
                msgList = null;
                this.next = null;
                return pCurLink.Read(ref pCurLink);
            }
            return msg;
        }
    }
}
