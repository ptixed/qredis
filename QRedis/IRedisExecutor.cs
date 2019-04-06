using System;
using System.Threading.Tasks;

namespace QRedis
{
    public interface IRedisExecutor : IDisposable
    {
        Task Execute(string queue, string message);
    }
}
