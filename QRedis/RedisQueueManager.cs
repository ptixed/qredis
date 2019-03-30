using QRedis.RedisModel;
using System;

namespace QRedis
{
    public class RedisQueueManager : RedisConnector
    {
        public static Action<string, Exception> ErrorHandler = delegate { };

        public RedisQueueManager(RedisServerConfig config)
            : base(config)
        {

        }

        public static string GetQueueName(string name) => nameof(QRedis) + "." + name;
        public static string GetProcessingQueueName(string name, string index) => nameof(QRedis) + "." + name + ".processing." + index;

        public RedisConsumer Consume(string name, int threads, Action<string> callback)
        {
            var ks = Request("KEYS", GetProcessingQueueName(name, "*"));
            if (!(ks is RedisArray ra))
                return null;

            // push not fully processed messages back to queue
            foreach (RedisBulkString k in ra)
                Request("RPOPLPUSH", k.Value, GetQueueName(name));

            return new RedisConsumer(name, _config, threads, callback);
        }
        
        public void Publish(string name, string message)
        {
            Request("LPUSH", GetQueueName(name), message);
        }
    }
}
