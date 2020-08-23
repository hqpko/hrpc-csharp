namespace com.haiswork.hrpc
{
    internal class Varint
    {
        internal static int PutVarint(byte[] buf,int index, int value)
        {
            return PutVarint(buf, index, (ulong)EncodeZigZag(value, 32));
        }
        
        internal static int PutVarint(byte[] buf,int index, ulong value)
        {
            do
            {
                var v = value & 0x7f;
                value >>= 7;
                if (value != 0)
                {
                    v |= 0x80;
                }

                buf[index++] = (byte) v;
            } while (value != 0);

            return index;
        }

        private static long EncodeZigZag(long value, int bitSize)
        {
            return (value << 1) ^ (value >> (bitSize - 1));
        }
        
        internal static long DecodeZigZag(ulong value)
        {
            if ((value & 0x1) == 0x1)
            {
                return (-1 * ((long)(value >> 1) + 1));
            }

            return (long)(value >> 1);
        }
        
    }
}