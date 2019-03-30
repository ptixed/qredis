namespace QRedis.RedisModel
{
    internal interface IRedisModel
    {
        void Serialize(RedisTokenWriter writer);
    }
}
