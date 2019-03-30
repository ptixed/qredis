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
        protected readonly RedisServerConfig _config;
        private Socket _socket;

        private bool _stopped;
        private readonly object _connectlock = new object();
        private readonly object _socketlock = new object();

        public RedisConnector(RedisServerConfig config)
        {
            _config = config;
            ReConnect();
        }

        private void ReConnect()
        {
            var socket = _socket;
            lock (_connectlock)
            { 
                if (socket != _socket)
                    return;

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
                        _socket.Connect(_config.Server, _config.Port);
                        if (_config.Passowrd == null || (Request(false, "AUTH", _config.Passowrd) as RedisSimpleString)?.Value == "OK")
                            return;
                    }
                    catch (Exception e) { RedisQueueManager.ErrorHandler(null, e); }

                    Thread.Sleep(_config.ReconnectTimeout);
                }
            }
        }

        internal IRedisModel Request(params string[] command) => Request(true, command);
        private IRedisModel Request(bool fixsocket, params string[] command)
        {
            var sb = new StringBuilder();
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
                catch (SocketException)
                {
                    if (!fixsocket)
                        return false;
                    ReConnect();
                }
                catch (IOException e)
                {
                    if (!_stopped)
                        RedisQueueManager.ErrorHandler("Unknown error in Send", e);
                }
                catch (ObjectDisposedException) { return false; }
                catch (Exception e) { RedisQueueManager.ErrorHandler("Unknown error in Send", e); }
        }

        private IRedisModel Receive(bool fixsocket)
        {
            try
            {
                var ret = RedisProtocol.Parse(new RedisTokenReader(new StreamReader(new NetworkStream(_socket))));
                if (ret is RedisError e)
                {
                    RedisQueueManager.ErrorHandler(e.Value, null);
                    return null;
                }
                return ret;
            }
            catch (SocketException)
            {
                if (!fixsocket)
                    return null;
                ReConnect();
            }
            catch (IOException e)
            {
                if (!_stopped)
                    RedisQueueManager.ErrorHandler("Unknown error in Receive", e);
            }
            catch (ObjectDisposedException) { return null; }
            catch (Exception e) { RedisQueueManager.ErrorHandler("Unknown error in Receive", e); }

            return null;
        }

        public void Dispose()
        {
            lock (_socketlock)
            {
                _stopped = true;
                _socket.Dispose();
            }
        }
    }
}
