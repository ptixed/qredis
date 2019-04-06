using QRedis.RedisModel;
using System;

namespace QRedis
{
    public class RedisQueueManager : RedisConnector
    {
        public static Action<string, Exception> ErrorHandler = delegate { };

        public readonly string InstanceName;

        public RedisQueueManager(string instance, RedisServerConfig config)
            : base(config)
        {
            InstanceName = instance;
        }

        public string GetQueueName(string queue) => $"{nameof(QRedis).ToLower()}.{queue}";
        public string GetProcessingQueueName(string queue, string index) => $"{nameof(QRedis).ToLower()}.{queue}.{InstanceName}.{index}";

        public IDisposable Consume(string queue, IRedisExecutor executor)
        {
            var ks = Request("KEYS", GetProcessingQueueName(queue, "*")) as RedisArray;
            if (ks == null)
                return null;

            // push not fully processed messages back to queue
            foreach (RedisBulkString k in ks)
                if (!Request("RPOPLPUSH", k.Value, GetQueueName(queue)).IsSuccess())
                    return null;

            return new RedisConsumer(this, executor, queue);
        }
        
        public bool Publish(string queue, string message)
        {
            return Request("LPUSH", GetQueueName(queue), message).IsSuccess();
        }
    }
}
