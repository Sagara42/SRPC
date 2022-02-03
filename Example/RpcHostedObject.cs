using System.Collections.Generic;
using System.Threading.Tasks;

namespace Example
{
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
}
