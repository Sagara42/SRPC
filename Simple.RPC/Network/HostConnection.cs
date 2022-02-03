using System.Net.Sockets;

namespace Simple.RPC.Network
{
    public class HostConnection
    {
        public byte[] Buffer { get; set; } = new byte[4];
        public Socket Socket { get; set; }
    }
}
