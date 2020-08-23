using System;
using System.Linq;

namespace com.haiswork.hrpc
{
    internal enum ReqType
    {
        Unknown,
        Call,
        OneWay,
        Reply
    };

    internal class HBuffer
    {
        private readonly byte[] _buf;
        private int _pos;

        internal static HBuffer NewBuffer(int cap)
        {
            return new HBuffer(cap);
        }
        
        internal static HBuffer NewBuffer(byte[] bytes)
        {
            return new HBuffer(bytes);
        }
        
        private HBuffer(int cap)
        {
            _buf = new byte[cap];
        }

        internal HBuffer WithHead()
        {
            _pos = 4;
            return this;
        }

        internal HBuffer UpdateHead()
        {
            var len = _pos - 4;
            _buf[0] = (byte) (len << 24);
            _buf[1] = (byte) (len << 16);
            _buf[2] = (byte) (len << 8);
            _buf[3] = (byte) (len);
            return this;
        }
        
        private HBuffer(byte[] buf)
        {
            _buf = buf;
        }

        internal byte ReadByte()
        {
            return _buf[_pos++];
        }
        
        internal int ReadInt()
        {
            var zigzag = ToTarget(32);
            return (int) Varint.DecodeZigZag(zigzag);
        }

        internal ulong ReadUlong()
        {
            return ToTarget(64);
        }

        internal byte[] GetBytes()
        {
            return _buf.Take(_pos).ToArray();
        }

        internal byte[] GetRestBytes()
        {
            return _buf.Skip(_pos).Take(_buf.Length - _pos).ToArray();
        }

        private ulong ToTarget(int sizeBites)
        {
            int shift = 0;
            ulong result = 0;

            while (_pos < _buf.Length)
            {
                var v = (ulong)_buf[_pos];
                _pos++;
                ulong tmp = v & 0x7f;
                result |= tmp << shift;

                if (shift > sizeBites)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if ((v & 0x80) != 0x80)
                {
                    return result;
                }

                shift += 7;
            }
            throw new ArgumentException();
        }

        internal HBuffer Write(byte b)
        {
            _buf[_pos++] = b;
            return this;
        }
        
        internal HBuffer Write(int i)
        {
            _pos = Varint.PutVarint(_buf, _pos, i);
            return this;
        }

        internal HBuffer Write(ulong u)
        {
            _pos = Varint.PutVarint(_buf, _pos, u);
            return this;
        }

        internal HBuffer Write(byte[] bytes)
        {
            Array.Copy(bytes, 0, _buf, _pos, bytes.Length);
            _pos += bytes.Length;
            return this;
        }
        
    }
}