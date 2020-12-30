using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace com.haiswork.hrpc
{
    public class Client
    {
        private const int MillisecondsTimeout = 8000;

        private ulong _seq;
        private Conn _conn;
        private int _millisecondsTimeout;
        private readonly Dictionary<ulong, Call> _dic = new Dictionary<ulong, Call>();

        private Action<int, byte[]> _handlerReceivedOneWay;

        public static async Task<Client> Connect(string host, int port, int millisecondsTimeout = -1)
        {
            if (millisecondsTimeout <= 0) millisecondsTimeout = MillisecondsTimeout;
            var conn = await Conn.Connect(host, port, millisecondsTimeout);
            return new Client
            {
                _millisecondsTimeout = millisecondsTimeout,
                _conn = conn
            };
        }

        public Client OnReceivedOneWay(Action<int, byte[]> handlerReceivedOneWay)
        {
            _handlerReceivedOneWay = handlerReceivedOneWay;
            return this;
        }

        public async void Run()
        {
            await _conn.OnReadPacket(OnReadPacket);
        }

        private void OnReadPacket(byte[] bytes)
        {
            var buf = HBuffer.NewBuffer(bytes);
            var respType = buf.ReadByte();
            switch (respType)
            {
                case (byte) ReqType.Reply:
                    var seq = buf.ReadUlong();
                    if (RemoveCall(seq, out var call))
                        call.SetResp(buf.GetRestBytes());
                    break;
                case (byte) ReqType.OneWay:
                    _handlerReceivedOneWay(buf.ReadInt(), buf.GetRestBytes());
                    break;
            }
        }

        public async Task Send(int pid, byte[] bytes)
        {
            await _conn.Send(CreatePacket(pid, bytes));
        }

        // 发送并等待应答
        public async Task<byte[]> Call(int pid, byte[] bytes)
        {
            var call = AddCall();
            await _conn.Send(CreatePacket(pid, _seq, bytes));

            return await call.RespTask.TimeoutAfter(_millisecondsTimeout);
        }

        private Call AddCall()
        {
            var call = new Call();
            lock (_dic)
            {
                _seq++;
                _dic[_seq] = call;
                return call;
            }
        }

        private bool RemoveCall(ulong seq, out Call call)
        {
            lock (_dic)
            {
                if (!_dic.TryGetValue(seq, out call)) return false;

                _dic.Remove(seq);
                return true;
            }
        }

        private static byte[] CreatePacket(int pid, ulong seq, byte[] bytes)
        {
            return HBuffer.NewBuffer(20 + bytes.Length).WithHead() // len:4+reqType:1+pid:5+seq:10 = 20
                .Write((byte) ReqType.Call)
                .Write(pid).Write(seq).Write(bytes)
                .UpdateHead().GetBytes();
        }

        private static byte[] CreatePacket(int pid, byte[] bytes)
        {
            return HBuffer.NewBuffer(10 + bytes.Length).WithHead() // len:4+reqType:1+pid:5=10
                .Write((byte) ReqType.OneWay)
                .Write(pid).Write(bytes)
                .UpdateHead().GetBytes();
        }
    }
}