using System;

namespace Simple.RPC.Network.Message
{
    public class RpcResponseMessage : IMessage
    {
        public Guid Identity { get; set; }
        public object Data { get; set; }
    }
}
