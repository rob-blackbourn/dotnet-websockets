using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace WebSocketServer
{
    class Program
    {
        static void Main(string [] args)
        {
            var listener = new TcpListener(IPAddress.Any, 8081);
            while (true)
            {
                var client = listener.AcceptTcpClient();
                var connection = new Connection(client);
                Task.Factory.StartNew(connection.Start);
            }
        }
    }
}

