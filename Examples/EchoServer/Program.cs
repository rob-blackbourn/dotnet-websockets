using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace EchoServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var listener = new TcpListener(IPAddress.Any, 8081);
            Console.WriteLine($"Listening at {listener.LocalEndpoint}");

            listener.Start();

            while (true)
            {
                var client = listener.AcceptTcpClient();

                Console.WriteLine("Socket accepted");

                var connection = new Connection(client);
                Task.Factory.StartNew(connection.Start);
            }
        }
    }
}

