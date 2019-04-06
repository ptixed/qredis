using System;

namespace QRedis
{
    public interface IRedisExecutor : IDisposable
    {
        void Enqueue(string queue, string message, Action<bool> whendone);
    }
}
