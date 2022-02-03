using Simple.RPC.Network.Message;
using Simple.RPC.Network.Serialization;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.RPC.Network
{
    public class RpcClient<T> : DispatchProxy where T : class
    {
        private string _host;
        private int _port;
        private bool _autoReconnect;

        private Socket _socket;
        private Thread _readThread;

        private readonly ConcurrentDictionary<Guid, RpcCallMessage> _messages = new();
        public T Proxy { get; set; }

        public RpcClient()
        {  
        }

        public static RpcClient<T> CreateProxy()
        {
            var proxy = Create<T, RpcClient<T>>();
            var instance = proxy as RpcClient<T>;
            instance.Proxy = proxy;
            return instance;
        }

        public void Connect(string host, int port, bool autoReconnect = true)
        {
            _host = host;
            _port = port;
            _autoReconnect = autoReconnect;
            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _socket.Connect(_host, _port);

            SetupReadThread();
        }

        private void SetupReadThread()
        {
            if(_readThread == null)
            {
                _readThread = new Thread(() =>
                {
                    while (true)
                    {
                        if (!_socket.Connected)
                        {
                            Thread.Sleep(100);

                            if (_autoReconnect)
                            {
                                try
                                {
                                    _socket.Connect(_host, _port);
                                }
                                catch { }
                            }
                        }
                        try
                        {
                            var lengthBuffer = new byte[4];

                            _socket.Receive(lengthBuffer);

                            var dataLength = BitConverter.ToUInt32(lengthBuffer);
                            var dataBuffer = new byte[dataLength];

                            var readed = 0;
                            while (readed < dataBuffer.Length)
                                readed += _socket.Receive(dataBuffer, readed, dataBuffer.Length - readed, SocketFlags.None);

                            var message = ApexSerializer.Instance.Deserialize<RpcResponseMessage>(dataBuffer);

                            _messages[message.Identity].Data = message.Data;
                            _messages[message.Identity].EventSlim.Set();
                        }
                        catch(Exception)
                        {
                            throw;
                        }
                    }
                });
                _readThread.Start();
            }
        }
       
        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            if (_socket == null || !_socket.Connected)
                throw new Exception("Connection not estabilished!");

#if DEBUG
            var stopWatch = Stopwatch.StartNew();
#endif
            var methodName = targetMethod.Name;
            var identity = Guid.NewGuid();
            var ev = new ManualResetEventSlim();

            var message = new RpcCallMessage
            {
                Identity = identity,
                MethodName = methodName,
                Data = args,
                EventSlim = ev 
            };

            _messages.TryAdd(identity, message);

            SendMessage(message);

            ev.Wait();

            _messages.TryRemove(identity, out var response);

#if DEBUG
            stopWatch.Stop();
            Debug.WriteLine($"[{methodName}] elapsed {stopWatch.Elapsed}");
#endif

            if (targetMethod.ReturnType.BaseType == typeof(Task))
            {           
                var genericArguments = targetMethod.ReturnType.GetGenericArguments();
                if(genericArguments.Length > 0)               
                    return CastResponseToTask(genericArguments[0], response.Data);               
            }

            return response.Data;
        }

        private void SendMessage(RpcCallMessage message)
        {
            var data = ApexSerializer.Instance.Serialize(message);
            var length = BitConverter.GetBytes(data.Length);
            var completeMessage = length.Concat(data).ToArray();

            _socket.Send(completeMessage);
        }

        private Task CastResponseToTask(Type type, object response)
        {
            if (type == typeof(float))
                return Task.FromResult((float)response);
            if (type == typeof(double))
                return Task.FromResult((double)response);
            if (type == typeof(decimal))
                return Task.FromResult((decimal)response);
            if (type == typeof(byte[]))
                return Task.FromResult((byte[])response);
            if (type == typeof(sbyte))
                return Task.FromResult((sbyte)response);
            if (type == typeof(byte))
                return Task.FromResult((byte)response);
            if (type == typeof(ushort))
                return Task.FromResult((ushort)response);
            if (type == typeof(short))
                return Task.FromResult((short)response);
            if (type == typeof(uint))
                return Task.FromResult((uint)response);
            if (type == typeof(int))
                return Task.FromResult((int) response);
            if (type == typeof(string))
                return Task.FromResult(response as string);
            if(type == typeof(object))
                return Task.FromResult(response);

            throw new Exception($"Not supported Task type {type.Name}");
        }
    }
}
