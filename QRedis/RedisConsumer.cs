using QRedis.RedisModel;
using System;
using System.Threading.Tasks;

namespace QRedis
{
    public class RedisConsumer : IDisposable
    {
        private readonly RedisConnector[] _connectors;
        private readonly Task[] _workers;

        private bool _stopped;

        private readonly Action<string> _callback;
        private readonly string _queue;
        
        public RedisConsumer(string name, RedisServerConfig config, int threads, Action<string> callback)
        {
            _callback = callback;
            _queue = RedisQueueManager.GetQueueName(name);

            _connectors = new RedisConnector[threads];
            _workers = new Task[threads];

            for (int i = 0; i < threads; ++i)
            {
                var ii = i;
                _connectors[i] = new RedisConnector(config);
                _workers[i] = Task.Run(() => Work(name, ii));
            }
        }

        private void Work(string name, int index)
        {
            var connector = _connectors[index];
            var pqueue = RedisQueueManager.GetProcessingQueueName(name, index.ToString());

            while (!_stopped)
            {
                try
                {
                    var response = connector.Request("BRPOPLPUSH", _queue, pqueue, 60.ToString());
                    if (response == null)
                        continue;
                    switch (response)
                    {
                        case RedisBulkString s:
                            try { _callback(s.Value); }
                            catch (Exception e) { RedisQueueManager.ErrorHandler("Error during message consumption", e); }
                            connector.Request("LPOP", pqueue);
                            break;
                        default:
                            RedisQueueManager.ErrorHandler("Unrecognized response from Redis: " + new RedisTokenWriter(response), null);
                            break;
                    }
                }
                catch (Exception e) { RedisQueueManager.ErrorHandler(null, e); }
            }
        }

        public void Dispose()
        {
            _stopped = true;
            foreach (var i in _connectors)
                i.Dispose();
            Task.WaitAll(_workers);
        }
    }
}
