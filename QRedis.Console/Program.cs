using System;
using System.Threading;
using static System.Console;

namespace QRedis.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var queue = "test";

            RedisQueueManager.ErrorHandler = (x, e) =>
            {
                WriteLine(x + " " + e);
            };

            var manager = new RedisQueueManager(Environment.MachineName, new RedisServerConfig
            {
                Passowrd = null,
                Port = 6379,
                ReconnectTimeout = TimeSpan.FromSeconds(10),
                Server = "192.168.56.101"
            });

            manager.Publish(queue, "1");
            manager.Publish(queue, "2");
            manager.Publish(queue, "3");

            var consumer = manager.Consume(queue, 2, x =>
            {
                Thread.Sleep(1000);
                WriteLine("received: " + x);
            });

            WriteLine("listening...");

            while (true)
            {
                var line = ReadLine();
                if (line == "exit")
                    break;
                if (manager.Publish(queue, line))
                    WriteLine("published!");
            }

            consumer.Dispose();
            manager.Dispose();

            WriteLine("finished");
            ReadLine();
        }
    }
}
