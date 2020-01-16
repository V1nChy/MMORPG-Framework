using System;
using UnityEngine;
using GFW;

namespace CodeX
{
    public class NetworkManager : ManagerModule
    {
        public SocketClient m_clients;
        private static SequenceMsg mEvents = new SequenceMsg();
        private NetworkInfo m_curr_read_buff;

        public override void Awake(MonoBehaviour mono)
        {
            m_clients = new SocketClient();
            m_clients.OnRegister(AddEvent);
        }

        private void AddEvent(int _event, ByteBuffer data)
        {
            mEvents.Add(new NetworkInfo(_event, data));
        }

        /// <summary>
        /// 交给Command，这里不想关心发给谁。
        /// </summary>
        public override void Update() {
            m_curr_read_buff = mEvents.Pop();
            while (m_curr_read_buff != null)
            {
                GameSystem.Instance.ExecuteCommand(GameCommandDef.SocketCommand, m_curr_read_buff);
                m_curr_read_buff = mEvents.Pop();
            }
        }

        /// <summary>
        /// 发送链接请求
        /// </summary>
        public void SendConnect() {
            m_clients.SendConnect(AppConst.SocketAddress, AppConst.SocketPort);
        }

        /// <summary>
        /// 发送SOCKET消息
        /// </summary>
        public void SendMessage(ByteBuffer buffer) {
            m_clients.SendMessage(buffer);
        }

        public override void Release()
        {
            m_clients.OnClose();
        }
    }
}