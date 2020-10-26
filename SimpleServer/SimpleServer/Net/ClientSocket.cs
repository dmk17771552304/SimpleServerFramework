using System.Net.Sockets;

namespace SimpleServer.Net
{
    public class ClientSocket
    {
        public Socket Socket;

        public long LastPingTime { get; set; }

        public ByteArray ReadBuffer = new ByteArray();

        public int UserId = 0;
    }
}
