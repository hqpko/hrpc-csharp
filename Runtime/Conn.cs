using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace com.haiswork.hrpc
{
    class Conn
    {
        private static int _defMaxBufferLen = 64 * 1024;// 64k
        private Socket _socket;
        private readonly byte[] _buffer = new byte[_defMaxBufferLen];
        private int _offset;
        internal async Task Connect(string host, int port)
        {
            _socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            var connectTask = Task.Factory.FromAsync(_socket.BeginConnect, _socket.EndConnect, host, port, null);
            await connectTask.ConfigureAwait(false);
        }

        internal async Task SendPacket(byte[] bytes)
        {
            var asyncResult = _socket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, null, null); 
            await Task.Factory.FromAsync(asyncResult, _ => _socket.EndSend(asyncResult));  
        }
        
        private async Task<byte[]> ReadOnePacket()
        {
            while (true)
            {
                var len = ReadPacketLen();
                if (len > _defMaxBufferLen)
                {
                    throw new Exception("over max reading size");
                }

                if (len > 0 && _offset >= len + 4)
                {
                    var packet = new byte[len];
                    Buffer.BlockCopy(_buffer,4,packet,0,len);
                    var remain = _offset - len - 4;
                    if (remain > 0)
                    {
                        Buffer.BlockCopy(_buffer,len+4,_buffer,0,remain);                        
                    }

                    _offset -= len + 4;
                    return packet;
                }

                var asyncResult = _socket.BeginReceive(_buffer, _offset, _buffer.Length - _offset, SocketFlags.None,
                    null, null);
                var readSize = await Task<int>.Factory.FromAsync(asyncResult, _ => _socket.EndReceive(asyncResult));
                _offset += readSize;
                if (readSize == 0)
                {
                    break;
                }
            }

            return null;
        }

        private int ReadPacketLen()
        {
            if (_offset > 4)
            {
                return _buffer[3] | _buffer[2] << 8 | _buffer[1] << 16 | _buffer[0] << 24;
            }

            return 0;
        }

        internal async Task OnReadPacket(Action<byte[]> handlerReadPacket)
        {
            while (true)
            {
                var packet = await ReadOnePacket();
                if (packet == null)
                {
                    break;
                }
                handlerReadPacket(packet);
            }
        }
    }
}