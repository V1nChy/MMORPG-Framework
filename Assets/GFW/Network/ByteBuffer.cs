using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;
using System;
using LuaInterface;

namespace GFW
{
    public class ByteBuffer
    {
        private MemoryStream stream = null;
        private BinaryWriter writer = null;
        private BinaryReader reader = null;

        public ByteBuffer()
        {
            this.stream = new MemoryStream();
            this.writer = new BinaryWriter(this.stream);
        }

        public ByteBuffer(byte[] data)
        {
            if (data != null)
            {
                stream = new MemoryStream(data);
                reader = new BinaryReader(stream);
            }
            else
            {
                stream = new MemoryStream();
                writer = new BinaryWriter(stream);
            }
        }

        public void Close()
        {
            if (writer != null)
            {
                writer.Close();
            }
            if (reader != null)
            {
                reader.Close();
            }
            stream.Close();
            writer = null;
            reader = null;
            stream = null;
        }

        public void WriteByte(byte v)
        {
            writer.Write(v);
        }

        public void WriteUshort(ushort v)
        {
            v = GFWEncoding.SwapUInt16(v);
            writer.Write(v);
        }

        public void WriteShort(short v)
        {
            v = GFWEncoding.SwapInt16(v);
            writer.Write(v);
        }

        public void WriteUint(uint v)
        {
            v = GFWEncoding.SwapUInt32(v);
            this.writer.Write(v);
        }

        public void WriteInt(int v)
        {
            v = GFWEncoding.SwapInt32(v);
            writer.Write(v);
        }

        public void WriteLong(long v)
        {
            v = GFWEncoding.SwapInt64(v);
            this.writer.Write(v);
        }

        public void WriteUlong(ulong v)
        {
            v = GFWEncoding.SwapUInt64(v);
            this.writer.Write(v);
        }

        public void WriteFloat(float v)
        {
            byte[] temp = BitConverter.GetBytes(v);
            Array.Reverse(temp);
            this.writer.Write(BitConverter.ToSingle(temp, 0));
        }

        public void WriteDouble(double v)
        {
            byte[] temp = BitConverter.GetBytes(v);
            Array.Reverse(temp);
            this.writer.Write(BitConverter.ToDouble(temp, 0));
        }

        public void WriteString(string v)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(v);
            ushort len = GFWEncoding.SwapUInt16((ushort)bytes.Length);
            this.writer.Write(len);
            this.writer.Write(bytes);
        }

        public void WriteText(string v)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(v);
            this.writer.Write(bytes);
        }

        public void WriteBytes(byte[] v)
        {
            int len = GFWEncoding.SwapInt32(v.Length);
            this.writer.Write(len);
            this.writer.Write(v);
        }

        public void WriteBuffer(LuaByteBuffer strBuffer)
        {
            this.WriteBytes(strBuffer.buffer);
        }

        public byte ReadByte()
        {
            return this.reader.ReadByte();
        }

        public short ReadShort()
        {
            return GFWEncoding.SwapInt16(this.reader.ReadInt16());
        }

        public ushort ReadUshort()
        {
            return GFWEncoding.SwapUInt16(this.reader.ReadUInt16());
        }

        public int ReadInt()
        {
            return GFWEncoding.SwapInt32(this.reader.ReadInt32());
        }

        public uint ReadUint()
        {
            return GFWEncoding.SwapUInt32(this.reader.ReadUInt32());
        }

        public long ReadLong()
        {
            return GFWEncoding.SwapInt64(this.reader.ReadInt64());
        }

        public ulong ReadUlong()
        {
            return GFWEncoding.SwapUInt64(this.reader.ReadUInt64());
        }

        public float ReadFloat()
        {
            byte[] temp = BitConverter.GetBytes(this.reader.ReadSingle());
            Array.Reverse(temp);
            return BitConverter.ToSingle(temp, 0);
        }

        public double ReadDouble()
        {
            byte[] temp = BitConverter.GetBytes(this.reader.ReadDouble());
            Array.Reverse(temp);
            return BitConverter.ToDouble(temp, 0);
        }

        public string ReadString()
        {
            ushort len = this.ReadUshort();
            byte[] buffer = new byte[(int)len];
            buffer = this.reader.ReadBytes((int)len);
            return Encoding.UTF8.GetString(buffer);
        }

        public byte[] ReadBytes()
        {
            uint len = this.ReadUint();
            return this.reader.ReadBytes((int)len);
        }

        public LuaByteBuffer ReadBuffer()
        {
            byte[] bytes = this.ReadBytes();
            return new LuaByteBuffer(bytes);
        }

        public byte[] ToBytes()
        {
            this.writer.Flush();
            return this.stream.ToArray();
        }

        public void Flush()
        {
            this.writer.Flush();
            this.writer.Dispose();
        }
    }
}