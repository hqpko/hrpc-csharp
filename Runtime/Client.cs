using System;
using System.Collections.Generic;
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
        internal Client(Conn conn)
        {
            _conn = conn;
        }
        // 发送并等待应答
        public async Task<byte[]> Call(TimeSpan timeout)
        {
            var call = new Call();
            _dic[++_seq] = call;
            var tokenSource = new CancellationTokenSource();
            if (await Task.WhenAny(call.RespTask,Task.Delay(timeout,tokenSource.Token)) == call.RespTask)
            {
                tokenSource.Cancel();
                return await call.RespTask;
            }
            else
            {
                throw new TimeoutException();
            }
        }

        // 仅发送
        public async Task Send()
        {
            await Task.Delay(10);
        }

        public async Task OnReadPacket(Action<byte[]> handlerReadPacket)
        {
            await _conn.OnReadPacket(handlerReadPacket);
        }
    }
}