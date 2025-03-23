using System;
using System.IO;
using System.Net.Sockets;

using WebSockets.Core;
using WebSockets.Core.Messages;

namespace EchoClient
{
    class Connection
    {
        private readonly Stream _stream;
        private readonly ClientHandshakeProtocol _handshakeProtocol;
        private readonly MessageProtocol _messageProtocol;

        public Connection(TcpClient client, string origin, string[] subProtocols)
        {
            _stream = client.GetStream();
            _handshakeProtocol = new ClientHandshakeProtocol(origin, subProtocols);
            _messageProtocol = new MessageProtocol(true);
        }

        public void Start()
        {
            PerformHandshake();
            ProcessMessages();
        }

        private void ProcessMessages()
        {
            Console.WriteLine("Processing messages.");
            Console.WriteLine("Sending 'close' will cause the server to initiate a close handshake.");
            Console.WriteLine("Sending 'CLOSE' will force the client to initiate a close handshake.");
            while (_messageProtocol.State == MessageProtocolState.Connected)
            {
                try
                {
                    Console.Write("Message (<ENTER> to quit): ");
                    var text = Console.ReadLine();
                    if (text is null || text == "")
                    {
                        // Exit without closing.
                        break;
                    }

                    if (text == "CLOSE")
                    {
                        // Send a message.
                        Console.WriteLine("Initiating close handshake");
                        WriteMessage(new CloseMessage(1000, "Client initiated close"));
                    }
                    else
                    {
                        // Send a message.
                        Console.WriteLine("Sending message");
                        WriteMessage(new TextMessage(text));
                    }

                    // Receive the echoed response.
                    var message = ReadMessage();
                    if (message.Type == MessageType.Text)
                    {
                        var textMessage = (TextMessage)message;
                        Console.WriteLine($"Received text message {textMessage.Text}");
                    }
                    else if (message.Type == MessageType.Close)
                    {
                        Console.WriteLine("Received close.");
                        if (_messageProtocol.State == MessageProtocolState.Closing)
                        {
                            // Send the close back.
                            Console.WriteLine("Responding with close (completing close handshake).");
                            WriteMessage(message);
                        }
                        else if (_messageProtocol.State == MessageProtocolState.Closed)
                        {
                            Console.WriteLine("Close handshake complete");
                        }
                        else
                        {
                            throw new InvalidOperationException($"Closed received from invalid state {_handshakeProtocol.State}");
                        }
                    }

                }
                catch (EndOfStreamException)
                {
                    Console.WriteLine("The server has dropped the connection");
                    break;
                }
                catch (InvalidOperationException error)
                {
                    Console.WriteLine($"Protocol error: {error.Message}");
                    break;
                }
            }
            Console.WriteLine("bye");
        }

        private Message ReadMessage()
        {
            var buffer = new byte[1024];
            while (true)
            {
                if (_messageProtocol.HasMessage)
                    return _messageProtocol.ReadMessage();

                var bytesRead = _stream.Read(buffer);
                if (bytesRead > 0)
                    _messageProtocol.WriteData(buffer, 0, bytesRead);
                else
                    throw new EndOfStreamException("The client closed the connection.");
            }
        }

        private void WriteMessage(Message message)
        {
            _messageProtocol.WriteMessage(message);

            var buffer = new byte[1024];
            while (_messageProtocol.HasData)
            {
                var offset = 0L;
                var bytesRead = _messageProtocol.ReadData(buffer, offset, buffer.Length);
                _stream.Write(buffer, 0, (int)bytesRead);
                Console.WriteLine("Sent client data");
            }
        }

        private WebResponse? PerformHandshake()
        {
            Console.WriteLine("Performing handshake");

            WriteHandshakeRequest("/chat", "www.example.com");
            var response = ReadHandshakeResponse();

            if (response is null || response.Code != 101)
            {
                Console.WriteLine("Handshake failed");
                throw new Exception("Failed to create WebSocket");
            }

            Console.WriteLine("Handshake completed");

            return response;
        }

        private void WriteHandshakeRequest(string path, string host)
        {
            Console.WriteLine("Sending handshake request");
            _handshakeProtocol.WriteRequest(path, host);

            var buffer = new byte[1024];
            while (_handshakeProtocol.HasData)
            {
                var bytesRead = _handshakeProtocol.ReadData(buffer);
                _stream.Write(buffer, 0, (int)bytesRead);
            }
        }

        private WebResponse? ReadHandshakeResponse()
        {
            Console.WriteLine("Receiving handshake response");
            var buffer = new byte[1024];
            WebResponse? webResponse = null;
            while (webResponse is null)
            {
                var bytesRead = _stream.Read(buffer);
                _handshakeProtocol.WriteData(buffer, 0, bytesRead);
                webResponse = _handshakeProtocol.ReadResponse();
            }

            return webResponse;
        }
    }
}