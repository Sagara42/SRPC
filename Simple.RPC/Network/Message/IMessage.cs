using System;

namespace Simple.RPC.Network.Message
{
    public interface IMessage
    {
        Guid Identity { get; set; }
        object Data { get; set; }
    }
}
