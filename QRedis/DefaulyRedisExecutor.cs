using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace QRedis
{
    public class DefaulyRedisExecutor : IRedisExecutor
    {
        internal class WorkItem
        {
            public string Queue;
            public string Message;
            public Semaphore Done = new Semaphore(0, 1);
            public bool Result;
        }

        private readonly Semaphore _semaphore = new Semaphore(0, int.MaxValue);
        private readonly ConcurrentQueue<WorkItem> _queue = new ConcurrentQueue<WorkItem>();
        private readonly Task[] _workers;
        private bool _stopped;

        private readonly Action<string, string> _callback;

        public DefaulyRedisExecutor(int threads, Action<string, string> callback)
        {
            _callback = callback;
            _workers = new Task[threads];
            for (int i = 0; i < threads; ++i)
                _workers[i] = Task.Run(() => Work());
        }

        protected virtual void Work()
        {
            while (true)
            {
                _semaphore.WaitOne();
                if (_stopped)
                    return;
                if (_queue.TryDequeue(out WorkItem item))
                {
                    try { _callback(item.Queue, item.Message); }
                    catch (Exception e) { RedisQueueManager.ErrorHandler("Error during message consumption", e); }

                    item.Result = true;
                    item.Done.Release();
                }
            }
        }

        public Task Execute(string queue, string message)
        {
            var item = new WorkItem
            {
                Queue = queue,
                Message = message
            };

            _queue.Enqueue(item);
            _semaphore.Release();
            item.Done.WaitOne();

            return item.Result;
        }

        public void Dispose()
        {
            _stopped = true;
            while (_queue.TryDequeue(out WorkItem item))
                item.Done.Release();

            _semaphore.Release(_workers.Length);
            Task.WaitAll(_workers);
        }

        // exempklifi priority

    }
}
