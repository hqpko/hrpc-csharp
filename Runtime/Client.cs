using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace com.haiswork.hrpc
{
    public class Client
    {
        private ulong _seq;
        private readonly Conn _conn;
        private readonly Dictionary<ulong, Call> _dic = new Dictionary<ulong, Call>();

        private Action<int, byte[]> _handlerReceivedOneWay;

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
                var buf = HBuffer.NewBuffer(bytes);
                var respType = buf.ReadByte();
                if (respType == (byte)ReqType.Reply)
                {
                    var seq = buf.ReadUlong();
                    if (_dic.ContainsKey(seq))
                    {
                        var call = _dic[seq];
                        _dic.Remove(seq);
                        call.SetResp(buf.GetRestBytes());
                    }
                }else if (respType == (byte) ReqType.OneWay)
                {
                    _handlerReceivedOneWay(buf.ReadInt(), buf.GetRestBytes());
                }
            });
        }
        
        // 发送并等待应答
        public async Task<byte[]> Call(int pid, byte[] bytes , TimeSpan timeout)
        {
            _seq++;
            var call = new Call();
            _dic[_seq] = call;
            await _conn.SendPacket(CreatePacket(pid, _seq, bytes));
            var tokenSource = new CancellationTokenSource();
            if (await Task.WhenAny(call.RespTask,Task.Delay(timeout,tokenSource.Token)) == call.RespTask)
            {
                tokenSource.Cancel();
                return await call.RespTask;
            }
            throw new TimeoutException();
        }

        public async Task OneWay(int pid, byte[] bytes)
        {
            await _conn.SendPacket(CreatePacket(pid, bytes));
        }
        
        private static byte[] CreatePacket(int pid, ulong seq, byte[] bytes)
        {
            return HBuffer.NewBuffer(20 + bytes.Length).WithHead() // pid:5+seq:10+len:4+reqType:1 = 20
                .Write((byte) ReqType.Call)
                .Write(pid).Write(seq).Write(bytes)
                .UpdateHead().GetBytes();
        }

        private static byte[] CreatePacket(int pid, byte[] bytes)
        {
            return HBuffer.NewBuffer(10 + bytes.Length).WithHead() // len:4+pid:5+reqType:1=10
                .Write((byte) ReqType.OneWay)
                .Write(pid).Write(bytes)
                .UpdateHead().GetBytes();
        }

        public void OnReceivedOneWay(Action<int, byte[]> handlerReceivedOneWay)
        {
            _handlerReceivedOneWay = handlerReceivedOneWay;
        }
        
    }
}