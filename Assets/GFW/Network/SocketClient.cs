using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;
using System.Text;

namespace GFW
{
    public enum DisType
    {
        Exception,
        Disconnect,
    }

    public delegate void MsgDispatch(int id, ByteBuffer data);

    public class SocketClient
    {
        private const int MAX_READ = 8192;

        public MemoryStream readStream = null;
        public BinaryReader reader = null;

        private Socket mSock = null;
        private Thread mSendThread = null;
        private Thread mRecvThread = null;
        private Queue<ByteBuffer> mSendBuffers = new Queue<ByteBuffer>();

        private bool mConnectState = false;
        private bool m_is_ipv6 = false;

        private MsgDispatch onDispatch;

        public void OnRegister(MsgDispatch dispatch)
        {
            onDispatch = dispatch;
            this.readStream = new MemoryStream();
            this.reader = new BinaryReader(this.readStream);
        }

        public void SendConnect(string ip, int port)
        {
            this.SendConnect_Thread(ip, port);
        }

        private void OnDisconnected(DisType dis, string msg)
        {
            this.mConnectState = false;
            int protocal = (dis == DisType.Exception) ? 102 : 103;
            ByteBuffer buffer = new ByteBuffer(Encoding.Default.GetBytes(msg));
            DispatchMessage(protocal, buffer);
        }

        private void DispatchMessage(int id, ByteBuffer data)
        {
            if(onDispatch != null)
            {
                onDispatch(id,data);
            }
        }

        public void SendMessage(ByteBuffer buffer)
        {
            if (this.mSock == null || !this.mSock.Connected || !this.mConnectState)
            {
                buffer.Close();
                buffer = null;
            }
            else
            {
                lock (this.mSendBuffers)
                {
                    this.mSendBuffers.Enqueue(buffer);
                }
            }
        }

        public void OnClose()
        {
            onDispatch = null;
            reader.Close();
            readStream.Close();
            this.Close();
        }

        private void Close()
        {
            this.CloseSocket();
            this.ReleaseThread();
        }

        private void CloseSocket()
        {
            lock (this.mSendBuffers)
            {
                this.mSendBuffers.Clear();
            }
            if (this.mSock != null && this.mConnectState)
            {
                if (this.mSock.Connected)
                {
                    this.mSock.Shutdown(SocketShutdown.Both);
                    this.mSock.Close();
                }
                this.mSock = null;
            }
            this.mConnectState = false;
        }

        public void SetIpv6State(bool state)
        {
            this.m_is_ipv6 = state;
        }

        #region - Socket
        private void SendConnect_Thread(string ip, int port)
        {
            AddressFamily net_type = AddressFamily.InterNetwork;
            if (this.m_is_ipv6)
            {
                net_type = AddressFamily.InterNetworkV6;
            }
            try
            {
                this.mSock = new Socket(net_type, SocketType.Stream, ProtocolType.Tcp);
                this.mSock.NoDelay = true;
                this.mSock.LingerState = new LingerOption(false, 0);
                this.mSock.SendTimeout = 1000;
                this.mSock.ReceiveBufferSize = 8192;
                this.mSock.BeginConnect(IPAddress.Parse(ip), port, new AsyncCallback(this.OnConnectThread), this);
            }
            catch (Exception e)
            {
                this.Close();
                LogMgr.LogError("network connect error:" + e.Message);
            }
        }

        private void OnConnectThread(IAsyncResult asr)
        {
            if (this.mSock != null)
            {
                this.mConnectState = true;
                this.mSock.EndConnect(asr);
                this.mRecvThread = new Thread(new ThreadStart(this.Received));
                this.mRecvThread.IsBackground = true;
                this.mRecvThread.Priority = System.Threading.ThreadPriority.AboveNormal;
                this.mRecvThread.Start();

                this.mSendThread = new Thread(new ThreadStart(this.SendData));
                this.mSendThread.IsBackground = true;
                this.mSendThread.Priority = System.Threading.ThreadPriority.AboveNormal;
                this.mSendThread.Start();

                DispatchMessage(101, new ByteBuffer());
            }
        }

        private void SendData()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(ms);
            while (this.mConnectState)
            {
                try
                {
                    if (this.mSendBuffers.Count == 0)
                    {
                        Thread.Sleep(1);
                        continue;
                    }
                    ByteBuffer buffer = null;
                    lock (this.mSendBuffers)
                    {
                        buffer = this.mSendBuffers.Dequeue();
                    }
                    if (buffer != null)
                    {
                        if (this.mSock != null && this.mSock.Connected)
                        {
                            byte[] message = buffer.ToBytes();
                            ms.SetLength(0L);
                            ms.Position = 0L;
                            ushort msglen = GFWEncoding.SwapUInt16((ushort)(message.Length + 4));
                            writer.Write(msglen);
                            ushort flag = GFWEncoding.SwapUInt16(1000);
                            writer.Write(flag);
                            writer.Write(message);
                            writer.Flush();
                            this.mSock.Send(ms.ToArray());
                        }
                        else
                        {
                            this.mConnectState = false;
                        }
                        buffer.Close();
                        buffer = null;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Socket SendData Error:" + ex.Message);
                }
                Thread.Sleep(1);
            }
        }

        private void Received()
        {
            byte[] data = new byte[MAX_READ];
            while (this.mConnectState)
            {
                try
                {
                    if (this.mSock != null && this.mSock.Connected)
                    {
                        Array.Clear(data, 0, data.Length);
                        int bytesRead = this.mSock.Receive(data);
                        if (bytesRead < 1)
                        {
                            this.mConnectState = false;
                            this.OnDisconnected(DisType.Exception, "Thread Received BytesRead < 1");
                            break;
                        }
                        this.OnReceive(data, bytesRead);
                    }
                    else
                    {
                        this.mConnectState = false;
                    }
                }
                catch (Exception ex)
                {
                    this.mConnectState = false;
                    this.OnDisconnected(DisType.Exception, "Thread Received Error:" + ex.Message);
                    break;
                }
                Thread.Sleep(1);
            }
        }

        private void OnReceive(byte[] bytes, int length)
        {
            this.readStream.Seek(0L, SeekOrigin.End);
            this.readStream.Write(bytes, 0, length);
            this.readStream.Seek(0L, SeekOrigin.Begin);
            while (this.readStream.Length - this.readStream.Position > 4L)
            {
                uint messageLen = this.reader.ReadUInt32();
                uint len = GFWEncoding.SwapUInt32(messageLen);
                len -= 4u;
                if (!(this.readStream.Length - this.readStream.Position >= (long)((ulong)len)))
                {
                    this.readStream.Position = this.readStream.Position - 4L;
                    break;
                }
                ByteBuffer buffer = new ByteBuffer(this.reader.ReadBytes((int)len));
                DispatchMessage(Protocal.Message, buffer);
            }
            byte[] leftover = this.reader.ReadBytes((int)(this.readStream.Length - this.readStream.Position));
            this.readStream.SetLength(0L);
            this.readStream.Write(leftover, 0, leftover.Length);
        }

        private void ReleaseThread()
        {
            if (this.mRecvThread != null)
            {
                this.mRecvThread.Abort();
                this.mRecvThread = null;
            }
            if (this.mSendThread != null)
            {
                this.mSendThread.Abort();
                this.mSendThread = null;
            }
        }
        #endregion
    }
}
