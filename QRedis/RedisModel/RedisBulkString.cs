namespace QRedis.RedisModel
{
    internal class RedisBulkString : IRedisModel
    {
        public const char Peek0 = '$';

        public readonly string Value;

        public RedisBulkString(string value)
        {
            Value = value;
        }

        public void Serialize(RedisTokenWriter writer)
        {
            writer.WritePeek0(Peek0);
            if (Value == null)
            {
                writer.WriteInt(-1);
            }
            else
            {
                writer.WriteInt(Value.Length);
                writer.WriteString(Value);
            }
        }

        public static RedisBulkString Parse(RedisTokenReader reader)
        {
            reader.ReadPeek0(Peek0);
            var len = reader.ReadInt();
            return new RedisBulkString(len == -1 ? null : reader.ReadString(len));
        }
    }
}
