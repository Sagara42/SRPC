using Simple.RPC.Network.Message;
using Simple.RPC.Network.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.RPC.Network
{
    public class RpcHost<T> where T : class
    {
        private readonly HashSet<Thread> _listeningThreads = new();
        private readonly Socket _listeningSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private object _hostObject;
        private bool _multithreadHandle;

        public RpcHost(T hostObject, bool multithreadHandle = true)
        {
            _hostObject = hostObject;
            _multithreadHandle = multithreadHandle;
        }

        public void Start(string host, int port, int acceptThreads = 1)
        {
            _listeningSocket.Bind(new IPEndPoint(IPAddress.Parse(host), port));
            _listeningSocket.Listen(15);

            for (int i = 0; i < acceptThreads; i++)
            {
                var th = new Thread(() => _listeningSocket.BeginAccept(AcceptCallback, null));
                th.Start();
                _listeningThreads.Add(th);
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            Socket sock = null;

            try
            {
                sock = _listeningSocket.EndAccept(ar);

                var connection = new HostConnection();

                connection.Socket = sock;
                connection.Socket.BeginReceive(connection.Buffer, 0, 4, SocketFlags.None, ReadCallback, connection);
            }
            catch (Exception)
            {
                sock?.Close();
            }

            _listeningSocket.BeginAccept(AcceptCallback, null);
        }

        private void ReadCallback(IAsyncResult ar)
        {
            var connection = (HostConnection) ar.AsyncState;

            try
            {
                var received = connection.Socket.EndReceive(ar, out var err);
                if (err != SocketError.Success || received <= 0)
                {
                    connection.Socket.Disconnect(false);
                    connection.Socket.Close();
                    return;
                }

                var buf = connection.Buffer;
                var len = BitConverter.ToUInt32(buf, 0);
                if (len > 0)
                {
                    var data = new byte[len];
                    var readed = 0;
                    while (readed < data.Length)
                        readed += connection.Socket.Receive(data, readed, data.Length - readed, SocketFlags.None);

                    var message = ApexSerializer.Instance.Deserialize<RpcCallMessage>(data);

                    if (_multithreadHandle)
                    {
                        ThreadPool.QueueUserWorkItem((wc) =>
                        {
                            HandleRemoteProcedureCall(connection, wc as RpcCallMessage);
                        }, message);
                    }
                    else
                        HandleRemoteProcedureCall(connection, message);
                }
                else
                {
                    connection.Socket.BeginReceive(connection.Buffer, 0, connection.Buffer.Length, SocketFlags.None, ReadCallback, connection);
                    return;
                }

                connection.Socket.BeginReceive(connection.Buffer, 0, connection.Buffer.Length, SocketFlags.None, ReadCallback, connection);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception)
            {
                if (connection != null)
                    Disconnect(connection);
            }
        }

        private void HandleRemoteProcedureCall(HostConnection connection, RpcCallMessage message)
        {
            var method = _hostObject
                .GetType()
                .GetMethod(message.MethodName);

            var result = method.Invoke(_hostObject, (object[])message.Data);

            if (method.ReturnType.BaseType == typeof(Task))
            {
                var r = result as Task;
                if (!r.IsCompleted)                
                    Task.WaitAll(r);

                result = (r as dynamic).Result;
            }

            var responseMessage = new RpcResponseMessage
            {
                Identity = message.Identity,
                Data = result
            };

            var data = ApexSerializer.Instance.Serialize(responseMessage);
            var length = BitConverter.GetBytes(data.Length);
            var completeMessage = length.Concat(data).ToArray();

            connection.Socket.Send(completeMessage);
        }

        public void Disconnect(HostConnection client)
        {
            try
            {
                client.Socket.BeginDisconnect(false, EndDisconect, client);
            }
            catch (Exception)
            {
            }
        }

        private void EndDisconect(IAsyncResult ar)
        {
            var connection = (HostConnection) ar.AsyncState;

            try
            {
                connection.Socket.EndDisconnect(ar);
            }
            catch (Exception)
            {
            }
            finally
            {
                connection.Socket.Close();
            }
        }
    }
}
