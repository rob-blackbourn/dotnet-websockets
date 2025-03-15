using System;
using System.IO;
using System.Net.Sockets;

using WebSockets.Core;
using WebSockets.Core.Messages;

namespace EchoServer
{
    class Connection
    {
        private readonly NetworkStream _stream;
        private readonly ServerHandshake _handshake;
        private readonly MessageProtocol _messageProtocol;

        public Connection(TcpClient client, string[] subProtocols)
        {
            _stream = client.GetStream();
            _handshake = new ServerHandshake(subProtocols);
            _messageProtocol = new MessageProtocol(false);
        }

        public void Start()
        {
            PerformHandshake();
            ProcessMessages();
        }

        private void ProcessMessages()
        {
            // Listen to messages coming in, and echo them back out.
            // If the message is the word "close", start the close handshake.
            var buffer = new byte[1024];
            while (_messageProtocol.State == ProtocolState.Connected)
            {
                Console.WriteLine("Waiting for a message");

                var bytesRead = _stream.Read(buffer);
                if (bytesRead > 0)
                    _messageProtocol.WriteData(buffer, 0, bytesRead);
                else
                {
                    Console.WriteLine("The client closed the connection.");
                    break;
                }

                var message = _messageProtocol.ReadMessage();
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
                        SendMessage(new CloseMessage(1000, "Server closed as requested"));
                    }
                    else
                    {
                        Console.WriteLine("Echoing message back to client");
                        SendMessage(message);
                    }
                }
                else if (message.Type == MessageType.Ping)
                {
                    Console.WriteLine("Received ping, sending pong");

                    var pingMessage = (PingMessage)message;
                    var pongMessage = new PongMessage(pingMessage.Data);
                    SendMessage(pongMessage);
                }
                else if (message.Type == MessageType.Close)
                {
                    Console.WriteLine("Received close.");
                    if (_messageProtocol.State == ProtocolState.Closing)
                    {
                        Console.WriteLine("Sending close (completing close handshake).");
                        SendMessage(message);
                    }
                    else if (_messageProtocol.State == ProtocolState.Closed)
                    {
                        Console.WriteLine("Close handshake complete");
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

            ReceiveHandshakeRequest();
            SendHandshakeResponse();

            Console.WriteLine("Handshake completed");
        }

        private void ReceiveHandshakeRequest()
        {
            Console.WriteLine("Receiving request");

            WebRequest? webRequest = null;
            var buffer = new byte[1024];
            while (webRequest is null)
            {
                var bytesRead = _stream.Read(buffer);
                if (bytesRead == 0)
                    throw new EndOfStreamException();
                _handshake.WriteData(buffer, 0, bytesRead);

                webRequest = _handshake.ReadRequest();
            }

            var webResponse = _handshake.CreateWebResponse(webRequest);

            _handshake.WriteResponse(webResponse);
        }

        private void SendHandshakeResponse()
        {
            bool isDone = false;
            while (!isDone)
            {
                var buffer = new byte[1024];
                var bytesRead = 0L;
                _handshake.ReadData(buffer, ref bytesRead, buffer.LongLength);
                if (bytesRead == 0)
                    isDone = true;
                else
                {
                    _stream.Write(buffer, 0, (int)bytesRead);
                    Console.WriteLine("Sent client data");
                }
            }
        }

        private void SendMessage(Message message)
        {
            _messageProtocol.WriteMessage(message);

            var buffer = new byte[1024];
            while (_messageProtocol.HasData)
            {
                var bytesRead = _messageProtocol.ReadData(buffer, 0, buffer.Length);
                _stream.Write(buffer, 0, (int)bytesRead);
                Console.WriteLine("Sent client data");
            }
        }
    }
}