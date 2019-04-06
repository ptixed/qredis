using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace QRedis
{
    public abstract class SimpleRedisExecutor : IRedisExecutor
    {
        internal class WorkItem
        {
            public string Queue;
            public string Message;
            public Semaphore WhenPickedUp = new Semaphore(0, 1);
            public Action<bool> WhenDone;
        }

        private readonly Semaphore _semaphore = new Semaphore(0, int.MaxValue);
        private readonly ConcurrentQueue<WorkItem> _queue = new ConcurrentQueue<WorkItem>();
        private readonly Task[] _workers;

        private bool _stopped;

        public SimpleRedisExecutor(int threads)
        {
            _workers = new Task[threads];
            for (int i = 0; i < threads; ++i)
                _workers[i] = Task.Run(() => Work());
        }

        protected abstract bool Execute(string queue, string message);

        protected virtual void Work()
        {
            while (true)
            {
                _semaphore.WaitOne();
                if (_stopped)
                    return;
                if (_queue.TryDequeue(out WorkItem item))
                {
                    bool result = false;
                    item.WhenPickedUp.Release();

                    try { result = Execute(item.Queue, item.Message); }
                    catch (Exception e) { RedisQueueManager.ErrorHandler("Error during message consumption", e); }

                    item.WhenDone(result);
                }
            }
        }

        public void Enqueue(string queue, string message, Action<bool> whendone)
        {
            var item = new WorkItem
            {
                Queue = queue,
                Message = message,
                WhenDone = whendone
            };

            _queue.Enqueue(item);
            _semaphore.Release();
            item.WhenPickedUp.WaitOne();
        }

        public void Dispose()
        {
            _stopped = true;

            while (_queue.TryDequeue(out WorkItem item))
            {
                item.WhenPickedUp.Release();
                item.WhenDone(false);
            }

            _semaphore.Release(_workers.Length);
            Task.WaitAll(_workers);
        }
    }
}
