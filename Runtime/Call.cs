using System.Threading.Tasks;

namespace com.haiswork.hrpc
{
    internal class Call
    {
        internal readonly Task<byte[]> RespTask;

        private byte[] _bytes;

        internal Call()
        {
            RespTask = new Task<byte[]>(() => _bytes);
        }

        internal void SetResp(byte[] bytes)
        {
            _bytes = bytes;
            RespTask.Start();
        }
    }

    public class Resp
    {
        public bool IsTimeout;
        public byte[] Bytes;

        internal Resp(bool timeout)
        {
            IsTimeout = timeout;
        }

        internal Resp(byte[] bytes)
        {
            Bytes = bytes;
        }
        
        public static Resp Ok(byte[] bytes)
        {
            return new Resp(bytes);
        }

        public static Resp Timeout()
        {
            return new Resp(true);
        }
    }
}