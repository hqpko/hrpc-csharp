using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

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
                }
                ShowBytes(bytes);
            });
        }
        
        // 发送并等待应答
        public async Task<byte[]> Call(int pid, byte[] bytes , TimeSpan timeout)
        {
            _seq++;
            var call = new Call();
            _dic[_seq] = call;
            ShowBytes(CreateReqPacket(pid, _seq, bytes));
            await _conn.SendPacket(CreateReqPacket(pid, _seq, bytes));
            var tokenSource = new CancellationTokenSource();
            if (await Task.WhenAny(call.RespTask,Task.Delay(timeout,tokenSource.Token)) == call.RespTask)
            {
                tokenSource.Cancel();
                return await call.RespTask;
            }
            throw new TimeoutException();
        }

        public static void ShowBytes(byte[] bytes)
        {
            Debug.Log("start -----------");
            for (var i = 0; i < bytes.Length; i++)
            {
                var requestType = bytes[0];
                if (requestType == (byte) ReqType.Call)
                {
                    
                }
                Debug.Log(bytes[i]);
            }
            Debug.Log("end -----------");
        }

        public async Task OneWay(int pid, byte[] bytes)
        {
            await _conn.SendPacket(CreateReqPacket(pid, _seq, bytes));
        }
        
        private byte[] CreateReqPacket(int pid, ulong seq, byte[] bytes)
        {
            return HBuffer.NewBuffer(25 + bytes.Length).CreatePacket(pid, seq, bytes);
        }

        private byte[] CreateReqPacket(int pid, byte[] bytes)
        {
            return HBuffer.NewBuffer(24 + bytes.Length).CreatePacket(pid, bytes);
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