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
            while (true)
            {
                var buffer = new byte[1024];
                var bytesRead = _stream.Read(buffer);
                _protocol.Receive(buffer, 0, bytesRead);

                while (_protocol.MessagesReceived.Count > 0)
                {
                    var message = _protocol.MessagesReceived.Dequeue();

                    if (message.Type == MessageType.Text)
                    {
                        var textMessage = ((TextMessage)message);
                        if (textMessage.Text == "close")
                            _protocol.SendMessage(new CloseMessage(1000, "Server closed as requested"));
                        else
                            _protocol.SendMessage(message);
                    }
                    else if (message.Type == MessageType.Ping)
                    {
                        var pingMessage = ((PingMessage)message);
                        var pongMessage = new PongMessage(pingMessage.Data);
                        _protocol.SendMessage(pongMessage);
                    }
                    else if (message.Type == MessageType.Close)
                    {
                        _protocol.SendMessage(message);
                    }

                    SendClientData();                    
                }
            }
        }

        private void PerformHandshake()
        {
            ReceiveHandshake();
            SendClientData();
        }

        private void ReceiveHandshake()
        {
            bool isHandshakeReceived = false;
            var buffer = new byte[1024];
            while (!isHandshakeReceived)
            {
                var bytesRead = _stream.Read(buffer);
                if (bytesRead == 0)
                    throw new EndOfStreamException();

                isHandshakeReceived = _protocol.Receive(buffer, 0, bytesRead);
            }
        }

        private void SendClientData()
        {
            while (_protocol.SendBuffer.Count > 0)
            {
                var data = _protocol.SendBuffer.Dequeue();
                _stream.Write(data);
            }
        }
    }
}