using System;

namespace QRedis
{
    public struct RedisServerConfig
    {
        public int Port;
        public string Server;
        public string Passowrd;
        public TimeSpan ReconnectTimeout;
    }
}
