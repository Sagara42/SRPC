using System.Collections.Generic;
using System.Threading.Tasks;

namespace Example
{
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
}
