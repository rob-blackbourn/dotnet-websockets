using System;
using System.IO;
using System.Net.Sockets;

using WebSockets.Core;
using WebSockets.Core.Messages;
using WebSockets.Core.Http;

namespace EchoServer
{
    class Connection
    {
        private readonly Stream _stream;
        private readonly ServerHandshakeProtocol _handshakeProtocol;
        private readonly MessageProtocol _messageProtocol;

        public Connection(TcpClient client, string[] subProtocols)
        {
            _stream = client.GetStream();
            _handshakeProtocol = new ServerHandshakeProtocol(subProtocols);
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
            // Respond to a ping with a pong.
            // If the message is the word "close", start the close handshake.
            while (_messageProtocol.State == MessageProtocolState.Connected)
            {
                Console.WriteLine("Waiting for a message");
                try
                {
                    var message = ReadMessage();

                    if (message.Type == MessageType.Text)
                    {
                        var textMessage = (TextMessage)message;

                        Console.WriteLine($"Received text message \"{textMessage.Text}\"");

                        if (textMessage.Text == "close")
                        {
                            Console.WriteLine("Initiating close handshake");
                            WriteMessage(new CloseMessage(1000, "Server closed as requested"));
                        }
                        else
                        {
                            Console.WriteLine("Echoing message back to client");
                            WriteMessage(message);
                        }
                    }
                    else if (message.Type == MessageType.Ping)
                    {
                        Console.WriteLine("Received ping, sending pong");

                        var pingMessage = (PingMessage)message;
                        var pongMessage = new PongMessage(pingMessage.Data);
                        WriteMessage(pongMessage);
                    }
                    else if (message.Type == MessageType.Close)
                    {
                        Console.WriteLine("Received close.");
                        if (_messageProtocol.State == MessageProtocolState.Closing)
                        {
                            Console.WriteLine("Sending close (completing close handshake).");
                            WriteMessage(message);
                        }
                        else if (_messageProtocol.State == MessageProtocolState.Closed)
                        {
                            Console.WriteLine("Close handshake complete");
                        }
                        else
                        {
                            throw new InvalidOperationException("Invalid state");
                        }
                    }
                }
                catch (EndOfStreamException)
                {
                    Console.WriteLine("Client disconnected");
                    break;
                }
                catch (InvalidOperationException error)
                {
                    Console.WriteLine($"Protocol error: {error.Message}");
                    Console.WriteLine("Disconnecting");
                    break;
                }
            }

            Console.WriteLine("Bye");
        }

        private void PerformHandshake()
        {
            Console.WriteLine("Performing handshake");

            var webRequest = ReadHandshakeRequest();
            var webResponse = _handshakeProtocol.CreateWebResponse(webRequest);
            WriteHandshakeResponse(webResponse);

            Console.WriteLine("Handshake completed");
        }

        private Request ReadHandshakeRequest()
        {
            Console.WriteLine("Receiving handshake request");

            var buffer = new byte[1024];
            while (true)
            {
                var bytesRead = _stream.Read(buffer);
                if (bytesRead == 0)
                    throw new EndOfStreamException();

                _handshakeProtocol.WriteData(buffer, 0, bytesRead);
                var webRequest = _handshakeProtocol.ReadRequest();
                if (webRequest is not null)
                    return webRequest;
            }
        }

        private void WriteHandshakeResponse(Response webResponse)
        {
            Console.WriteLine("Sending handshake response");

            _handshakeProtocol.WriteResponse(webResponse);

            var buffer = new byte[1024];
            while (_handshakeProtocol.HasData)
            {
                var bytesRead = _handshakeProtocol.ReadData(buffer);
                _stream.Write(buffer, 0, (int)bytesRead);
                Console.WriteLine("Sent client data");
            }
        }

        private Message ReadMessage()
        {
            var buffer = new byte[1024];
            while (true)
            {
                if (_messageProtocol.HasMessage)
                    return _messageProtocol.ReadMessage();

                var bytesRead = _stream.Read(buffer);
                if (bytesRead == 0)
                    throw new EndOfStreamException("The client closed the connection.");

                _messageProtocol.WriteData(buffer, 0, bytesRead);
            }
        }

        private void WriteMessage(Message message)
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