using Simple.RPC.Network;
using Simple.RPC.Network.Serialization;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Example
{
    internal class Program
    {
        private static string _host = "127.0.0.1";
        private static int _port = 9060;

        static void Main()
        {
            ApexDefaultSettings.DefaultSettings.MarkSerializable(typeof(TestClass)); //marking needed if u use custom classes
            
            SetupHost();
            
            var sw = Stopwatch.StartNew();

            TestSimple();
            TestParallel();
            TestAsync();

            Console.WriteLine($"Done {sw.Elapsed}");
        }

        private static void SetupHost()
        {
            var host = new RpcHost<RpcHostedObject>(new RpcHostedObject(), multithreadHandle: false);
            host.Start(_host, _port);
        }

        private static void TestParallel()
        {
            var client = RpcClient<IHostedObject>.CreateProxy();
            client.Connect(_host, _port);

            Parallel.For(0, 100, (i) => { client.Proxy.GetTestClass(); });
        }

        private static void TestSimple()
        {
            var client = RpcClient<IHostedObject>.CreateProxy();
            client.Connect(_host, _port);

            client.Proxy.GetInt();
            client.Proxy.GetTestClass();
        }

        private static async void TestAsync()
        {
            var client = RpcClient<IHostedObject>.CreateProxy();
            client.Connect(_host, _port);

            await client.Proxy.GetStringAsync();
            await client.Proxy.GetObjectAsync();
        }
    }
}
