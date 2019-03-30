namespace QRedis.RedisModel
{
    internal class RedisSimpleString : IRedisModel
    {
        public const char Peek0 = '+';

        public readonly string Value;

        public RedisSimpleString(string value)
        {
            Value = value;
        }

        public void Serialize(RedisTokenWriter writer)
        {
            writer.WritePeek0(Peek0);
            writer.WriteString(Value);
        }

        public static RedisSimpleString Parse(RedisTokenReader reader)
        {
            reader.ReadPeek0(Peek0);
            return new RedisSimpleString(reader.ReadString());
        }
    }
}
