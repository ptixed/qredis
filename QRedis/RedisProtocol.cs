using QRedis.RedisModel;

namespace QRedis
{
    static internal class RedisProtocol
    {
        public const string Delimiter = "\r\n";

        public static IRedisModel Parse(RedisTokenReader reader)
        {
            var peek0 = reader.ReadPeek0();

            switch (peek0)
            {
                case RedisSimpleString.Peek0:
                    return RedisSimpleString.Parse(reader);
                case RedisInteger.Peek0:
                    return RedisInteger.Parse(reader);
                case RedisError.Peek0:
                    return RedisError.Parse(reader);
                case RedisBulkString.Peek0:
                    return RedisBulkString.Parse(reader);
                case RedisArray.Peek0:
                    return RedisArray.Parse(reader);
            }

            return null;
        }

        public static bool IsSuccess(this IRedisModel model)
        {
            return model != null && !(model is RedisError);
        }
    }
}
