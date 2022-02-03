using Simple.RPC.Network;
using Simple.RPC.Network.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Example
{
    internal class Program
    {
        private static string _host = "127.0.0.1";
        private static int _port = 9060;

        static void Main(string[] args)
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

            var someInt = client.Proxy.GetInt();
            var someClass = client.Proxy.GetTestClass();
        }

        private static async void TestAsync()
        {
            var client = RpcClient<IHostedObject>.CreateProxy();
            client.Connect(_host, _port);

            var someStr = await client.Proxy.GetStringAsync();
            var obj = await client.Proxy.GetObjectAsync() as TestClass;
        }
    }

    public interface IHostedObject
    {
        int GetInt();
        void Test();
        List<string> GetStrings();
        TestClass GetTestClass();
        Task TestAsync();
        Task<string> GetStringAsync();
        Task<object> GetObjectAsync();
    }

    public class RpcHostedObject : IHostedObject
    {
        public int GetInt() { return 1; }

        public Task<object> GetObjectAsync()
        {
            return Task.FromResult(new TestClass() as object);
        }

        public async Task<string> GetStringAsync()
        {
            await Task.Delay(1000);

            return "string data";
        }

        public List<string> GetStrings()
        {
            return new List<string>
            {
                { "Test1" }, { "Test2" }
            };
        }

        public TestClass GetTestClass()
        {
            return new TestClass();
        }

        public void Test()
        {
        }

        public async Task TestAsync()
        {
            await Task.Delay(1000);
        }
    }

    public class TestClass
    {
        public int SomeInt { get; set; } = 25;
    }
}
