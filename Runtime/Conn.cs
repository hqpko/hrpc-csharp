using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace com.haiswork.hrpc
{
    internal class Conn
    {
        private const int MaxBufferSize = 64 * 1024; // 64k
        private const int HeadSize = 4;

        private int _timeoutMilliseconds;
        private Socket _socket;
        private readonly byte[] _buffer = new byte[MaxBufferSize];
        private int _offset;

        internal static async Task<Conn> Connect(string host, int port, int timeoutMilliseconds)
        {
            var conn = new Conn
            {
                _timeoutMilliseconds = timeoutMilliseconds,
                _socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp)
            };
            await Task.Run(() => conn._socket.Connect(host, port)).TimeoutAfter(timeoutMilliseconds);
            return conn;
        }

        internal async Task Send(byte[] bytes)
        {
            await Task.Run(() => _socket.Send(bytes)).TimeoutAfter(_timeoutMilliseconds);
        }

        private async Task<byte[]> MustReadOnePacket()
        {
            while (true)
            {
                var packet = ReadOnePacket();
                if (packet != null) return packet;

                var readSize = await Task.Run(
                    () => _socket.Receive(_buffer, _offset, _buffer.Length - _offset, SocketFlags.None)
                ).TimeoutAfter(_timeoutMilliseconds);
                _offset += readSize;
                if (readSize == 0) return null; // return null if socket closed
            }
        }

        private byte[] ReadOnePacket()
        {
            if (_offset < 4) return null;
            var len = _buffer[3] | _buffer[2] << 8 | _buffer[1] << 16 | _buffer[0] << 24;
            if (len > MaxBufferSize) throw new Exception("over max reading size");
            if (len <= 0 || _offset < len + HeadSize) return null;

            var packetBytes = new byte[len];
            Buffer.BlockCopy(_buffer, HeadSize, packetBytes, 0, len);
            var packetSize = _offset - len - HeadSize;
            if (packetSize > 0) Buffer.BlockCopy(_buffer, len + HeadSize, _buffer, 0, packetSize);
            _offset -= len + HeadSize;
            return packetBytes;
        }

        internal async Task OnReadPacket(Action<byte[]> handlerReadPacket)
        {
            while (true)
            {
                var packet = await MustReadOnePacket();
                if (packet == null) break;
                handlerReadPacket(packet);
            }
        }
    }
}