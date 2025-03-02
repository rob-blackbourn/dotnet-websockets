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
            _protocol = new ServerProtocol([]);
        }

        public void Start()
        {
            PerformHandshake();

            // Listen to messages coming in, and echo them back out.
            // If the message is the word "close", start the close handshake.
            while (_protocol.State == ConnectionState.Connected)
            {
                Console.WriteLine("Waiting for a message");

                var buffer = new byte[1024];
                var bytesRead = _stream.Read(buffer);
                if (bytesRead == 0)
                {
                    Console.WriteLine("The client closed the connection.");
                    break;;
                }
                _protocol.WriteMessageData(buffer, 0, bytesRead);

                var message = _protocol.ReadMessage();
                if (message is null)
                {
                    Console.WriteLine("Not enough data for message");
                    continue;
                }

                if (message.Type == MessageType.Text)
                {
                    var textMessage = (TextMessage)message;

                    Console.WriteLine($"Received text message \"{textMessage.Text}\"");

                    if (textMessage.Text == "close")
                    {
                        Console.WriteLine("Initiating close handshake");

                        _protocol.WriteMessage(new CloseMessage(1000, "Server closed as requested"));
                        SendClientData();
                    }
                    else
                    {
                        Console.WriteLine("Echoing message back to client");

                        _protocol.WriteMessage(message);
                        SendClientData();
                    }
                }
                else if (message.Type == MessageType.Ping)
                {
                    Console.WriteLine("Received ping, sending pong");

                    var pingMessage = ((PingMessage)message);
                    var pongMessage = new PongMessage(pingMessage.Data);
                    _protocol.WriteMessage(pongMessage);
                    SendClientData();
                }
                else if (message.Type == MessageType.Close)
                {
                    Console.WriteLine("Received close.");
                    if (_protocol.State == ConnectionState.Closing)
                    {
                        Console.WriteLine("Sending close (completing close handshake).");
                        _protocol.WriteMessage(message);
                        SendClientData();
                    } else if (_protocol.State == ConnectionState.Closed)
                    {
                        Console.WriteLine("Received close, done");
                    }
                    else
                    {
                        throw new InvalidOperationException("Invalid state");
                    }
                }
            }

            Console.WriteLine("Bye");
        }

        private void PerformHandshake()
        {
            Console.WriteLine("Performing handshake");

            ReceiveHandshake();
            SendHandshakeData();

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
                _protocol.WriteHandshakeData(buffer, 0, bytesRead);

                isHandshakeReceived = _protocol.Handshake();
            }
        }

        private void SendHandshakeData()
        {
            bool isDone = false;
            while (!isDone)
            {
                var buffer = new byte[1024];
                var bytesRead = _protocol.ReadHandshakeData(buffer);
                if (bytesRead == 0)
                    isDone = true;
                else
                {
                    _stream.Write(buffer, 0, (int)bytesRead);
                    Console.WriteLine("Sent client data");
                }
            }
        }

        private void SendClientData()
        {
            var isDone = false;
            var buffer = new byte[1024];
            var offset = 0L;
            while (!isDone)
            {
                isDone = _protocol.ReadMessageData(buffer, ref offset, buffer.Length);
                _stream.Write(buffer, 0, (int)offset);
                Console.WriteLine("Sent client data");
            }
        }
    }
}