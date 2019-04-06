using QRedis.RedisModel;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace QRedis
{
    public class RedisConnector : IDisposable
    {
        public readonly RedisServerConfig Config;
        private Socket _socket;

        private bool _stopped;
        private readonly object _socketlock = new object();

        private readonly object _requestlock = new object();

        public RedisConnector(RedisServerConfig config)
        {
            Config = config;
            Connect(false);
        }

        private void Connect(bool retry)
        {
            while (true)
            {
                lock (_socketlock)
                {
                    if (_stopped)
                        return;
                    if (_socket != null)
                        _socket.Dispose();
                    _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                }

                try
                {
                    _socket.Connect(Config.Server, Config.Port);
                    if (Config.Passowrd == null || (Request(false, "AUTH", Config.Passowrd) as RedisSimpleString)?.Value == "OK")
                        return;
                }
                catch (Exception e) { RedisQueueManager.ErrorHandler(null, e); }

                if (!retry)
                    throw new Exception("Cannot connect to Redis instance");

                Thread.Sleep(Config.ReconnectTimeout);
            }
        }

        internal virtual IRedisModel Request(params string[] command)
        {
            lock (_requestlock)
                return Request(true, command);
        }

        private IRedisModel Request(bool fixsocket, params string[] command)
        {
            var rarray = new RedisArray(command.Select(x => new RedisBulkString(x)));
            var writer = new RedisTokenWriter(rarray);

            var success = Send(fixsocket, Encoding.UTF8.GetBytes(writer.ToString()));
            if (!success)
                return null;

            return Receive(fixsocket);
        }

        private bool Send(bool fixsocket, byte[] data)
        {
            while (true)
                try
                {
                    _socket.Send(data);
                    return true;
                }
                catch (Exception e)
                {
                    if (_stopped)
                        return false;

                    if (e is IOException || e is SocketException)
                    {
                        if (fixsocket)
                            Connect(true);
                    }
                    else
                    {
                        RedisQueueManager.ErrorHandler("Error in Send", e);
                        return false;
                    }
                }
        }

        private IRedisModel Receive(bool fixsocket)
        {
            try
            {
                var ret = RedisProtocol.Parse(new RedisTokenReader(new StreamReader(new NetworkStream(_socket))));
                if (ret is RedisError e)
                    RedisQueueManager.ErrorHandler(e.Value, null);
                return ret;
            }
            catch (Exception e)
            {
                if (_stopped)
                    return null;

                if (e is IOException || e is SocketException)
                {
                    if (fixsocket)
                        Connect(true);
                }
                else
                    RedisQueueManager.ErrorHandler("Error in Receive", e);

                return null;
            }
        }

        public virtual void Dispose()
        {
            lock (_socketlock)
            {
                _stopped = true;
                _socket.Dispose();
            }
        }
    }
}
