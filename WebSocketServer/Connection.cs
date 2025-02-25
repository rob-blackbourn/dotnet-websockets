using System;
using System.IO;
using System.Net.Sockets;

using WebSockets.Core;

namespace WebSocketServer
{
    class Connection
    {
        private readonly NetworkStream _stream;
        private readonly ServerProtocol _protocol;

        public Connection(TcpClient client)
        {
            _stream = client.GetStream();
            _protocol = new ServerProtocol();
        }

        public void Start()
        {
            PerformHandshake();

            // Listen to messages coming in, and echo them back out.
            // If the message is the word "close", start the close handshake.
            var ok = true;
            while (ok)
            {
                Console.WriteLine("Waiting for a message");

                var buffer = new byte[1024];
                var bytesRead = _stream.Read(buffer);
                if (bytesRead == 0)
                {
                    Console.WriteLine("The client closed the connection.");
                    ok = false;
                    continue;
                }
                _protocol.Submit(buffer, 0, bytesRead);

                var isDone = false;
                while (!isDone)
                {
                    var message = _protocol.Process();
                    if (message is null)
                    {
                        isDone = true;
                        continue;
                    }

                    if (message.Type == MessageType.Text)
                    {
                        var textMessage = (TextMessage)message;

                        Console.WriteLine($"Received text message \"{textMessage.Text}\"");
 
                        if (textMessage.Text == "close")
                        {
                            Console.WriteLine("initiating close handshake");

                            _protocol.SendMessage(new CloseMessage(1000, "Server closed as requested"));
                        }
                        else
                        {
                            Console.WriteLine("Echoing message back to client");

                            _protocol.SendMessage(message);
                        }
                    }
                    else if (message.Type == MessageType.Ping)
                    {
                        Console.WriteLine("Received ping, sending pong");

                        var pingMessage = ((PingMessage)message);
                        var pongMessage = new PongMessage(pingMessage.Data);
                        _protocol.SendMessage(pongMessage);
                    }
                    else if (message.Type == MessageType.Close)
                    {
                        Console.WriteLine("Received close, sending close");

                        _protocol.SendMessage(message);
                    }

                    SendClientData();                    
                }
            }

            Console.WriteLine("Bye");
        }

        private void PerformHandshake()
        {
            Console.WriteLine("Performing handshake");

            ReceiveHandshake();
            SendClientData();

            Console.WriteLine("Handshake completed");
        }

        private void ReceiveHandshake()
        {
            Console.WriteLine("Receiving request");

            bool isHandshakeReceived = false;
            var buffer = new byte[1024];
            while (!isHandshakeReceived)
            {
                var bytesRead = _stream.Read(buffer);
                if (bytesRead == 0)
                    throw new EndOfStreamException();
                _protocol.Submit(buffer, 0, bytesRead);

                isHandshakeReceived = _protocol.Handshake();
            }
        }

        private void SendClientData()
        {
            while (_protocol.IsWriteable)
            {
                var buffer = new byte[1024];
                var offset = 0L;
                if (_protocol.Write(buffer, ref offset))
                {
                    // This cast is safe. The offset must be less than
                    // the buffer length, which is an int.
                    _stream.Write(buffer, 0, (int)offset);
                }

                Console.WriteLine("Sent client data");
            }
        }
    }
}