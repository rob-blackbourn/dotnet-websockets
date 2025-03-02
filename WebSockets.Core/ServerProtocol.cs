using System;
using System.IO;
using System.Linq;
using System.Text;

namespace WebSockets.Core
{
    /// <summary>
    /// A sans-io implementation of the server side of the WebSocket protocol.
    /// 
    /// The business layer logic is not provided. For example when a ping is
    /// received, the implementer is expected to return the pong. This is also
    /// the case for a close.
    /// </summary>
    public class ServerProtocol : Protocol
    {
        public ServerProtocol(string[] subProtocols)
            :   this(subProtocols, new DateTimeProvider(), new NonceGenerator())
        {
        }

        public ServerProtocol(
            string[] subProtocols,
            IDateTimeProvider dateTimeProvider,
            INonceGenerator nonceGenerator)
            :   base(false, subProtocols, dateTimeProvider, nonceGenerator)
        {
        }

        public bool Handshake()
        {
            if (!_handshakeBuffer.EndsWith(HTTP_EOM))
                return false;

            try
            {
                var text = Encoding.UTF8.GetString(_handshakeBuffer.ToArray());
                var webRequest = WebRequest.Parse(text);

                if (webRequest.Verb != "GET")
                    throw new InvalidDataException("Expected GET request");

                if (webRequest.Version != "HTTP/1.1")
                    throw new InvalidDataException("Expected version HTTP/1.1");
                    
                if (webRequest.Headers.SingleValue("Connection")?.ToLowerInvariant() != "upgrade")
                    throw new InvalidDataException("Expected connection header to be \"upgrade\"");
                    
                if (webRequest.Headers.SingleValue("Upgrade")?.ToLowerInvariant() != "websocket")
                    throw new InvalidDataException("Expected upgrade header to be \"websocket\"");

                var key = webRequest.Headers.SingleValue("Sec-WebSocket-Key"); 
                if (key is null)
                    throw new InvalidDataException("Mandatory header Sec-WebSocket-Key missing");

                var version = webRequest.Headers.SingleValue("Sec-WebSocket-Version"); 
                if (version is null)
                    throw new InvalidDataException("Mandatory header Sec-WebSocket-Version missing");
                if (version != "13")
                    throw new InvalidDataException("Unsupported version");

                var subProtocols = webRequest.Headers.SingleCommaValues("Sec-WebSocket-Protocol");

                SendHandshakeResponse(key, subProtocols);
                State = ConnectionState.Connected;
                return true;

            }
            catch (InvalidDataException error)
            {
                SendBadRequest(error.Message);
                State = ConnectionState.Faulted;
                return true;
            }
        }

        private void SendHandshakeResponse(string requestKey, string[]? candidateSubProtocols)
        {
            var builder = new StringBuilder();

            builder.Append("HTTP/1.1 101 Switching Protocols\r\n");
            builder.Append("Upgrade: websocket\r\n");
            builder.Append("Connection: Upgrade\r\n");

            var subProtocol = NegotiateSubProtocols(candidateSubProtocols);
            if (subProtocol is not null)
                builder.AppendFormat("Sec-WebSocket-Protocol: {0}\r\n", subProtocol);

            builder.AppendFormat("Sec-WebSocket-Accept: {0}\r\n", CreateResponseKey(requestKey));

            builder.Append("\r\n");

            var text = builder.ToString();
            var data = Encoding.ASCII.GetBytes(text);
            WriteHandshakeData(data, 0, data.Length);
        }

        private string? NegotiateSubProtocols(string[]? candidateSubProtocols)
        {
            if (_subProtocols.Length == 0 || candidateSubProtocols is null || candidateSubProtocols.Length == 0)
                return null;

            var matches = candidateSubProtocols.Intersect(_subProtocols).ToList();
            if (matches.Count == 0)
                throw new InvalidDataException("No requested protocols supported");

            return matches[0];
        }

        private void SendBadRequest(string reason)
        {
            var body = Encoding.UTF8.GetBytes(reason);
            var header = Encoding.UTF8.GetBytes(
                "HTTP/1.1 400 Bad Request\r\n" +
                $"Date: {_dateTimeProvider.Now.ToUniversalTime():r}\r\n" +
                "Connection: close\r\n" +
                $"Content-Length: {body.Length}\r\n" +
                "Content-Type: text/plain; charset=utf-8\r\n" +
                "\r\n"
            );
            var data = new byte[header.Length + body.Length];
            Array.Copy(header, data, header.Length);
            Array.Copy(body, 0, data, header.Length, body.Length);
            WriteHandshakeData(data, 0, data.Length);
        }
    }
}