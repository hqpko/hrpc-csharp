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
}