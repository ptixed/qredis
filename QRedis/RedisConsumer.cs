using QRedis.RedisModel;
using System.Threading.Tasks;

namespace QRedis
{
    internal class RedisConsumer : RedisConnector
    {
        private readonly RedisQueueManager _manager;
        private readonly IRedisExecutor _executor;
        private readonly string _queue;

        private readonly Task _task;
        private bool _stopped;

        public RedisConsumer(RedisQueueManager manager, IRedisExecutor executor, string queue)
            : base (manager.Config)
        {
            _manager = manager;
            _executor = executor;
            _queue = manager.GetQueueName(queue);

            _task = Task.Run(() => Work());
        }

        private void Work()
        {
            for (int index = 0; !_stopped; ++index)
            {
                var pqueue = _manager.GetProcessingQueueName(_queue, index.ToString());

                RedisBulkString response = null;
                while (response == null)
                    response = Request("BRPOPLPUSH", _queue, pqueue, 60.ToString()) as RedisBulkString;

                _executor.Execute(_queue, response.Value)
                    .ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            if (_manager.Request("RPOPLPUSH", _queue, pqueue, 60.ToString()) as RedisBulkString == null)
                                RedisQueueManager.ErrorHandler("Cannot mark message as undone", null);
                        }
                        else
                        {
                            if (!_manager.Request("LPOP", pqueue).IsSuccess())
                                RedisQueueManager.ErrorHandler("Cannot mark message as done", null);
                        }
                    });
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _stopped = true;
            _task.Wait();
        }
    }
}
