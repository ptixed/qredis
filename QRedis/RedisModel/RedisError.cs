namespace QRedis.RedisModel
{
    internal class RedisError : IRedisModel
    {
        public const char Peek0 = '-';

        public readonly string Value;

        public RedisError(string value)
        {
            Value = value;
        }

        public void Serialize(RedisTokenWriter writer)
        {
            writer.WritePeek0(Peek0);
            writer.WriteString(Value);
        }

        public static RedisError Parse(RedisTokenReader reader)
        {
            reader.ReadPeek0(Peek0);
            return new RedisError(reader.ReadString());
        }
    }
}
