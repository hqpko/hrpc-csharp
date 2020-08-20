using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace com.haiswork.hrpc
{
    public class Client
    {
        private ulong _seq = 0;
        private Conn _conn;
        private Dictionary<ulong, Call> _dic = new Dictionary<ulong, Call>();

        public static async Task<Client> Connect(string host, int port)
        {
            var conn = new Conn();
            await conn.Connect(host, port);
            var client = new Client(conn);
            return client;
        }
        
        private Client(Conn conn)
        {
            _conn = conn;
        }

        public async void Run()
        {
            await _conn.OnReadPacket(bytes =>
            {

            });
        }
        
        // 发送并等待应答
        public async Task<byte[]> Call(Int32 pid, byte[] bytes , TimeSpan timeout)
        {
            _seq++;
            var call = new Call();
            _dic[_seq] = call;
            var tokenSource = new CancellationTokenSource();
            if (await Task.WhenAny(call.RespTask,Task.Delay(timeout,tokenSource.Token)) == call.RespTask)
            {
                tokenSource.Cancel();
                return await call.RespTask;
            }
            throw new TimeoutException();
        }
        
        private byte[] CreateReqPacket(int pid, long seq, byte[] bytes)
        {
            var req = new byte[24+bytes.Length]; // len:4+pid(varint):10+seq(varint):10=24
            int index = 4;
            // write pid
            index = PutVarintInt(req, index, pid);

            // write seq
            index = PutVarintLong(req, index, seq);
            
            // write bytes
            Array.Copy(bytes, 0, req, index, bytes.Length);
            int len = index + bytes.Length - 4;
            
            req[0] = (byte) (len << 24);
            req[1] = (byte) (len << 16);
            req[2] = (byte) (len << 8);
            req[3] = (byte) (len);

            return req.Take(len + 4).ToArray();
        }

        private int PutVarintLong(byte[] buf, int index, long value)
        {
            return PutVarint(buf, index, (ulong)EncodeZigZag(value, 64));
        }
        
        private int PutVarintInt(byte[] buf,int index, int value)
        {
            return PutVarint(buf, index, (ulong)EncodeZigZag(value, 32));
        }
        
        private int PutVarint(byte[] buf,int index, ulong value)
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

        private long EncodeZigZag(long value, int bitSize)
        {
            return (value << 1) ^ (value >> (bitSize - 1));
        }

        // 仅发送
        public async Task Send()
        {
            await Task.Delay(10);
        }

        public void OnReceivedOneWay(Action<byte[]> handlerReceivedOneWay)
        {
            
        }

        public async Task OnReadPacket(Action<byte[]> handlerReadPacket)
        {
            await _conn.OnReadPacket(handlerReadPacket);
        }
    }
}