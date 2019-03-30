using System;

namespace QRedis
{
    public class RedisServerConfig
    {
        public int Port;
        public string Server;
        public string Passowrd;
        public TimeSpan ReconnectTimeout = TimeSpan.FromSeconds(1);
    }
}
