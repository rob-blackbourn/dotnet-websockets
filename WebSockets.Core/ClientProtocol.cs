using System;
using System.IO;
using System.Linq;
using System.Text;

namespace WebSockets.Core
{
    /// <summary>
    /// A sans-io implementation of the client side of the WebSocket protocol.
    /// 
    /// The business layer logic is not provided. For example when a ping is
    /// received, the implementer is expected to return the pong. This is also
    /// the case for a close.
    /// </summary>
    public class ClientProtocol : Protocol
    {
        private readonly string _origin;
        private readonly string _key;
        private string? _selectedSubProtocol;

        public ClientProtocol(string origin, string[] subProtocols)
            :   this(origin, subProtocols, new DateTimeProvider(), new NonceGenerator())
        {
        }

        public ClientProtocol(
            string origin,
            string[] subProtocols,
            IDateTimeProvider dateTimeProvider,
            INonceGenerator nonceGenerator)
            :   base(true, subProtocols, dateTimeProvider, nonceGenerator)
        {
            _origin = origin;
            _key = nonceGenerator.CreateClientKey();
        }

        public bool ReadMessageData(byte[] buffer, ref long offset, long length)
        {
            return _messageWriter.ReadMessageData(buffer, ref offset, length);
        }

        public Message? ReadMessage()
        {
            var message = _messageReader.ReadMessage();
            if (message is null)
                return null;

            if (message.Type == MessageType.Close)
            {
                if (State == ConnectionState.Connected)
                {
                    State = ConnectionState.Closing;
                }
                else if (State == ConnectionState.Closing)
                {
                    State = ConnectionState.Closed;
                }
                else
                {
                    throw new InvalidOperationException("received close message when not connected or closing");
                }
            }

            return message;
        }

        public void SendHandshakeRequest(string path, string host)
        {
            var builder = new StringBuilder();

            builder.AppendFormat("GET {0} HTTP/1.1\r\n", path);
            builder.AppendFormat("Host: {0}\r\n", host);
            builder.Append("Upgrade: websocket\r\n");
            builder.Append("Connection: Upgrade\r\n");
            builder.AppendFormat("Sec-WebSocket-Key: {0}\r\n", _key);

            if (_subProtocols is not null && _subProtocols.Length > 0)
                builder.AppendFormat("Sec-WebSocket-Protocol: {0}\r\n", string.Join(',', _subProtocols));

            builder.Append("Sec-WebSocket-Version: 13\r\n");

            builder.AppendFormat("Origin: {0}\r\n", _origin);

            builder.Append("\r\n");

            var text = builder.ToString();
            var data = Encoding.ASCII.GetBytes(text);
            WriteHandshakeData(data, 0, data.Length);
        }

        public bool ProcessHandshakeResponse()
        {
             if (!_handshakeBuffer.EndsWith(HTTP_EOM))
                return false;

            var text = Encoding.UTF8.GetString(_handshakeBuffer.ToArray());
            var webResponse = WebResponse.Parse(text);

            if (webResponse.Version != "HTTP/1.1")
                throw new InvalidDataException("Expected version HTTP/1.1");

            if (webResponse.Code != 101)
                throw new InvalidDataException("Expected response code 101");
                
            if (webResponse.Headers.SingleValue("Connection")?.ToLowerInvariant() != "upgrade")
                throw new InvalidDataException("Expected connection header to be \"upgrade\"");
                
            if (webResponse.Headers.SingleValue("Upgrade")?.ToLowerInvariant() != "websocket")
                throw new InvalidDataException("Expected upgrade header to be \"websocket\"");

            var accept = webResponse.Headers.SingleValue("Sec-WebSocket-Accept"); 
            if (accept is null)
                throw new InvalidDataException("Mandatory header Sec-WebSocket-Accept missing");
            
            var expected = CreateResponseKey(_key);
            if (accept != expected)
                throw new InvalidDataException("Invalid Sec-WebSocket-Accept token");

            _selectedSubProtocol = webResponse.Headers.SingleValue("Sec-WebSocket-Protocol");

            State = ConnectionState.Connected;
            return true;           
        }
    }
}