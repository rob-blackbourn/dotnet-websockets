using System;
using System.Net;
using System.Net.Sockets;
using WebSockets.Core;

namespace  WebSocketClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 8081);

            Console.WriteLine($"Connecting to {endpoint}");

            var tcpClient = new TcpClient();
            tcpClient.Connect(endpoint);

            var connection = new Connection(tcpClient, "brick.jetblack.net", []);
            connection.Start();
        }
    }    
}