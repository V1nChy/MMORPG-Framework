using System;

namespace GFW
{
    public class NetworkInfo
    {
        public int id;
        public ByteBuffer buffer;

        public NetworkInfo(int _id, ByteBuffer _buffer)
        {
            id = _id;
            buffer = _buffer;
        }
    }
}
