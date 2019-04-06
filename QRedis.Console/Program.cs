using System;
using System.Threading;
using static System.Console;

namespace QRedis.Console
{
    class MyRedisExecutor : SimpleRedisExecutor
    {
        private Random _random = new Random();

        public MyRedisExecutor()
            : base(threads: 5)
        {

        }

        protected override bool Execute(string queue, string message)
        {
            // processing can take a while
            Thread.Sleep(_random.Next(2000));

            // and it does not always succeed
            if (_random.Next(100) > 50)
            {
                WriteLine(".");
                return false;
            }

            WriteLine("processing successfull: " + message);
            return true;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            RedisQueueManager.ErrorHandler = (x, e) => WriteLine(x + " " + e);

            var queue = "test";
            var config = new RedisServerConfig
            {
                Passowrd = null,
                Port = 6379,
                ReconnectTimeout = TimeSpan.FromSeconds(10),
                Server = "192.168.56.101"
            };

            using (var manager = new RedisQueueManager(Environment.MachineName, config))
            {
                for (int i = 0; i < 10; ++i)
                    manager.Publish(queue, i.ToString());

                using (var executor = new MyRedisExecutor())
                using (var consumer = manager.Consume(queue, executor))
                {
                    WriteLine("listening...");
                    while (true)
                    {
                        var line = ReadLine();
                        if (line == "exit")
                            break;
                        if (manager.Publish(queue, line))
                            WriteLine(line + " published!");
                    }
                }
            }

            WriteLine("finished");
            ReadLine();
        }
    }
}
