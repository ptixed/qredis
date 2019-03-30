namespace QRedis.RedisModel
{
    internal class RedisInteger : IRedisModel
    {
        public const char Peek0 = ':';

        public readonly long Value;

        public RedisInteger(long value)
        {
            Value = value;
        }

        public void Serialize(RedisTokenWriter writer)
        {
            writer.WritePeek0(Peek0);
            writer.WriteInt(Value);
        }

        public static RedisInteger Parse(RedisTokenReader reader)
        {
            reader.ReadPeek0(Peek0);
            return new RedisInteger(reader.ReadInt());
        }
    }
}
