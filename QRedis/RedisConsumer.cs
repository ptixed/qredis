using QRedis.RedisModel;
using System.Threading;
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

        private int _working;
        private object _workinglock = new object();

        public RedisConsumer(RedisQueueManager manager, IRedisExecutor executor, string queue)
            : base (manager.Config)
        {
            _manager = manager;
            _executor = executor;
            _queue = queue;

            _task = Task.Run(() => Work());
        }

        private void Work()
        {
            for (int index = 0; !_stopped; ++index)
            {
                var queue = _manager.GetQueueName(_queue);
                var wipqueue = _manager.GetProcessingQueueName(_queue, index.ToString());

                RedisBulkString response = null;
                while (response == null)
                {
                    response = Request("BRPOPLPUSH", queue, wipqueue, 60.ToString()) as RedisBulkString;
                    if (_stopped)
                        return;
                }

                lock (_workinglock)
                    ++_working;

                _executor.Enqueue(queue, response.Value, result =>
                {
                    if (result)
                    {
                        if (!_manager.Request("LPOP", wipqueue).IsSuccess())
                            RedisQueueManager.ErrorHandler("Cannot mark message as done", null);
                    }
                    else
                    {
                        if (!_manager.Request("RPOPLPUSH", wipqueue, queue).IsSuccess())
                            RedisQueueManager.ErrorHandler("Cannot mark message as undone", null);
                    }

                    lock (_workinglock)
                    {
                        --_working;
                        Monitor.Pulse(_workinglock);
                    }
                });
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _stopped = true;
            _task.Wait();

            lock (_workinglock)
            {
                if (_working > 0)
                    Monitor.Enter(_workinglock);
            }
        }
    }
}
