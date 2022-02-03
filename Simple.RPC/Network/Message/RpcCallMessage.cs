using System;
using System.Threading;

namespace Simple.RPC.Network.Message
{
    public class RpcCallMessage : IMessage
    {
        public Guid Identity { get; set; }
        public object Data { get; set; }
        public string MethodName { get; set; }

        [NonSerialized] public ManualResetEventSlim EventSlim;
    }
}
