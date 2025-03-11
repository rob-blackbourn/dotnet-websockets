using System;
using System.Net.Sockets;

using WebSockets.Core;
using WebSockets.Core.Messages;

namespace EchoClient
{
    class Connection
    {
        private readonly NetworkStream _stream;
        private readonly ClientHandshake _handshakeProtocol;
        private readonly MessageProtocol _messageProtocol;

        public Connection(TcpClient client, string origin, string[] subProtocols)
        {
            _stream = client.GetStream();
            _handshakeProtocol = new ClientHandshake(origin, subProtocols);
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
            while (_messageProtocol.State == ProtocolState.Connected)
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
                    SendMessage(new CloseMessage(1000, "Client initiated close"));
                }
                else
                {
                    // Send a message.
                    Console.WriteLine("Sending message");
                    SendMessage(new TextMessage(text));
                }

                // Receive the echoed response.
                var message = ReceiveMessage();
                if (message.Type == MessageType.Text)
                {
                    var textMessage = (TextMessage)message;
                    Console.WriteLine($"Received text message {textMessage.Text}");
                }
                else if (message.Type == MessageType.Close)
                {
                    Console.WriteLine("Received close.");
                    if (_messageProtocol.State == ProtocolState.Closing)
                    {
                        // Send the close back.
                        Console.WriteLine("Responding with close (completing close handshake).");
                        SendMessage(message);
                    }
                    else if (_messageProtocol.State == ProtocolState.Closed)
                    {
                        Console.WriteLine("Close handshake complete");
                    }
                    else
                    {
                        throw new InvalidOperationException($"Closed received from invalid state {_handshakeProtocol.State}");
                    }
                }
            }
            Console.WriteLine("bye");
        }

        private Message ReceiveMessage()
        {
            Message? message = null;
            while (message is null)
            {
                message = _messageProtocol.ReadMessage();
                if (message is not null)
                    continue;

                var buffer = new byte[1024];
                var bytesRead = _stream.Read(buffer);
                _messageProtocol.WriteData(buffer, 0, bytesRead);
            }
            return message;
        }

        private void SendMessage(Message message)
        {
            _messageProtocol.WriteMessage(message);

            var isDone = false;
            var buffer = new byte[1024];
            while (!isDone)
            {
                var offset = 0L;
                isDone = _messageProtocol.ReadData(buffer, ref offset, buffer.Length);
                _stream.Write(buffer, 0, (int)offset);
                Console.WriteLine("Sent client data");
            }
        }

        private void PerformHandshake()
        {
            Console.WriteLine("Performing handshake");

            SendHandshakeRequest();
            ReceiveHandshakeResponse();

            Console.WriteLine("Handshake completed");
        }

        private void SendHandshakeRequest()
        {
            Console.WriteLine("Sending handshake request");
            _handshakeProtocol.WriteRequest("/chat", "www.example.com");

            var buffer = new byte[1024];
            var isDone = false;
            while (!isDone)
            {
                var bytesRead = 0L;
                _handshakeProtocol.ReadData(buffer, ref bytesRead, buffer.LongLength);
                if (bytesRead == 0)
                    isDone = true;
                else
                    _stream.Write(buffer, 0, (int)bytesRead);
            }
        }

        private void ReceiveHandshakeResponse()
        {
            Console.WriteLine("Receiving handshake response");
            var buffer = new byte[1024];
            var offset = 0L;
            var isDone = false;
            while (!isDone)
            {
                var bytesRead = _stream.Read(buffer);
                _handshakeProtocol.WriteData(buffer, offset, bytesRead);
                if (offset == bytesRead)
                    offset = 0;
                isDone = _handshakeProtocol.ReadResponse() is not null;
            }
        }
    }
}