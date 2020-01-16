using System;

namespace GFW
{
    public class GFWEncoding
    {
        public static char[] digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };
        public static String BytesToHex(byte[] bytes, int size = 0)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            if (size <= 0 || size > bytes.Length)
            {
                size = bytes.Length;
            }

            char[] buf = new char[2 * size];
            for (int i = 0; i < size; i++)
            {
                byte b = bytes[i];
                buf[2 * i + 1] = digits[b & 0xF];
                b = (byte)(b >> 4);
                buf[2 * i + 0] = digits[b & 0xF];
            }
            return new String(buf);
        }

        public static byte[] HexToBytes(String s)
        {

            int len = s.Length;
            byte[] data = new byte[len / 2];
            for (int i = 0; i < len; i += 2)
            {

                data[i / 2] = (byte)((CharToValue(s[i]) << 4) + (CharToValue(s[i + 1])));
            }
            return data;
        }

        private static byte CharToValue(char ch)
        {
            if (ch >= '0' && ch <= '9')
            {
                return (byte)(ch - '0');
            }
            else if (ch >= 'a' && ch <= 'f')
            {
                return (byte)(ch - 'a' + 10);
            }
            else if (ch >= 'A' && ch <= 'F')
            {
                return (byte)(ch - 'A' + 10);
            }

            return 0;
        }

        public static int XORCodec(byte[] buffer, int begin, int len,  byte[] key)
        {
            if (buffer == null || key == null || key.Length == 0)
            {
                return -1;
            }

            if (begin + len >= buffer.Length)
            {
                return -1;
            }

            int blockSize = key.Length;
            int j = 0;
            for (j = begin; j < begin + len; j++)
            {
                buffer[j] = (byte)(buffer[j] ^ key[(j - begin) % blockSize]);
            }

            return j;
        }

        public static int XORCodec(byte[] inBytes, byte[] outBytes, byte[] keyBytes)
        {
            if (inBytes == null || outBytes == null || keyBytes == null || keyBytes.Length == 0)
            {
                return -1;
            }

            if (outBytes.Length < inBytes.Length)
            {
                return -1;
            }

            int blockSize = keyBytes.Length;
            int j = 0;
            for (j = 0; j < inBytes.Length; j++)
            {
                outBytes[j] = (byte)(inBytes[j] ^ keyBytes[j % blockSize]);
            }

            return j;
        }

        public static ushort CheckSum(byte[] buffer, int size)
        {
            ulong sum = 0;
            int i = 0;
            while (size > 1)
            {
                sum = sum + BitConverter.ToUInt16(buffer, i);
                size -= 2;
                i += 2;
            }
            if (size > 0)
            {
                sum += buffer[i];
            }

            while ((sum >> 16) != 0)
            {
                sum = (sum >> 16) + (sum & 0xffff);
            }

            return (ushort)(~sum);
        }

        public static short SwapInt16(short n)
        {
            return (short)((int)(n & 255) << 8 | (n >> 8 & 255));
        }

        public static ushort SwapUInt16(ushort n)
        {
            return (ushort)((int)(n & 255) << 8 | (n >> 8 & 255));
        }

        public static int SwapInt32(int n)
        {
            return ((int)SwapInt16((short)n) & 65535) << 16 | ((int)SwapInt16((short)(n >> 16)) & 65535);
        }

        public static uint SwapUInt32(uint n)
        {
            return (uint)((int)(SwapUInt16((ushort)n) & ushort.MaxValue) << 16 | (int)(SwapUInt16((ushort)(n >> 16)) & ushort.MaxValue));
        }

        public static long SwapInt64(long n)
        {
            return (((long)SwapInt32((int)n) & uint.MaxValue << 32) | ((long)SwapInt32((int)(n >> 32)) & uint.MaxValue));
        }

        public static ulong SwapUInt64(ulong n)
        {
            return ((ulong)(SwapUInt32((uint)n) & uint.MaxValue) << 32 | ((ulong)SwapUInt32((uint)(n >> 32)) & uint.MaxValue));
        }
    }
}
