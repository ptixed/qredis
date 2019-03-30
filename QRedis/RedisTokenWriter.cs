using QRedis.RedisModel;
using System.Text;

namespace QRedis
{
    internal class RedisTokenWriter
    {
        private StringBuilder _sb = new StringBuilder();

        public RedisTokenWriter(IRedisModel model)
        {
            model.Serialize(this);
        }

        public void WritePeek0(char c)
        {
            _sb.Append(c);
        }

        public void WriteInt(long i)
        {
            _sb.Append(i);
            _sb.Append(RedisProtocol.Delimiter);
        }

        public void WriteString(string s)
        {
            _sb.Append(s);
            _sb.Append(RedisProtocol.Delimiter);
        }

        public override string ToString()
        {
            return _sb.ToString();
        }
    }
}
