using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using WebSockets.Core;

namespace WebSocketClient
{
    class Connection
    {
        private readonly NetworkStream _stream;
        private readonly ClientProtocol _protocol;

        public Connection(TcpClient client, string origin, string[] subProtocols)
        {
            _stream = client.GetStream();
            _protocol = new ClientProtocol(origin, subProtocols);
        }

        public void Start()
        {
            Console.WriteLine("Performing handshake");
            PerformHandshake();

            Console.WriteLine("Processing messages.");
            Console.WriteLine("Sending 'close' will cause the server to initiate a close handshake.");
            Console.WriteLine("Sending 'CLOSE' will force the client to initiate a close handshake.");
            while (_protocol.State == ConnectionState.Connected)
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
                    _protocol.WriteMessage(new CloseMessage(1000, "Client initiated close"));
                    SendData();
                }
                else
                {
                    // Send a message.
                    Console.WriteLine("Sending message");
                    _protocol.WriteMessage(new TextMessage(text));
                    SendData();
                }

                // Receive the echoed response.
                var response = ReceiveMessage();
                if (response.Type == MessageType.Text)
                {
                    var textMessage = (TextMessage)response;
                    Console.WriteLine($"Received text message {textMessage.Text}");
                }
                else if (response.Type == MessageType.Close)
                {
                    Console.WriteLine("Received close.");
                    if (_protocol.State == ConnectionState.Closing)
                    {
                        // Send the close back.
                        Console.WriteLine("Responding with close (completing close handshake).");
                        _protocol.WriteMessage(response);
                        SendData();
                    }
                    else if (_protocol.State == ConnectionState.Closed)
                    {
                        Console.WriteLine("Close handshake complete");
                    }
                    else
                    {
                        throw new InvalidOperationException($"Closed received from invalid state {_protocol.State}");
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
                message = _protocol.ReadMessage();
                if (message is not null)
                    continue;

                var buffer = new byte[1024];
                var bytesRead = _stream.Read(buffer);
                _protocol.WriteMessageData(buffer, 0, bytesRead);
            }
            return message;
        }

        private void SendData()
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

        private void PerformHandshake()
        {
            SendHandshakeRequest();
            ReceiveHandshakeResponse();
        }

        private void SendHandshakeRequest()
        {
            Console.WriteLine("Sending handshake request");
            _protocol.SendHandshakeRequest("/chat", "www.example.com");
            var buffer = new byte[1024];
            var isDone = false;
            while (!isDone)
            {
                var bytesRead = _protocol.ReadHandshakeData(buffer);
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
                _protocol.WriteHandshakeData(buffer, offset, bytesRead);
                if (offset == bytesRead)
                    offset = 0;
                isDone = _protocol.ProcessHandshakeResponse();
            }

        }

    }
}