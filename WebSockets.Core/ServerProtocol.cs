using System;
using System.Collections.Generic;
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

        public WebRequest? ReadHandshakeRequest()
        {
            if (!_handshakeBuffer.EndsWith(HTTP_EOM))
                return null;

            var text = Encoding.UTF8.GetString(_handshakeBuffer.ToArray());
            var webRequest = WebRequest.Parse(text);

            return webRequest;
        }

        public bool WriteHandshakeResponse(WebRequest webRequest)
        {
            try
            {
                var data = CreateHandshakeResponse(webRequest);
                WriteHandshakeData(data, 0, data.Length);

                State = ConnectionState.Connected;
                return true;

            }
            catch (InvalidDataException error)
            {
                var data = BuildErrorResponse(error.Message);
                WriteHandshakeData(data, 0, data.Length);

                State = ConnectionState.Faulted;
                return true;
            }
        }

        private byte[] CreateHandshakeResponse(WebRequest webRequest)
        {
            if (webRequest.Verb != "GET")
                throw new InvalidDataException("Expected GET request");

            if (webRequest.Version != "HTTP/1.1")
                throw new InvalidDataException("Expected version HTTP/1.1");
                
            if (webRequest.Headers.SingleValue("Connection")?.ToLowerInvariant() != "upgrade")
                throw new InvalidDataException("Expected connection header to be \"upgrade\"");
                
            if (webRequest.Headers.SingleValue("Upgrade")?.ToLowerInvariant() != "websocket")
                throw new InvalidDataException("Expected upgrade header to be \"websocket\"");

            var requestKey = webRequest.Headers.SingleValue("Sec-WebSocket-Key"); 
            if (requestKey is null)
                throw new InvalidDataException("Mandatory header Sec-WebSocket-Key missing");

            var version = webRequest.Headers.SingleValue("Sec-WebSocket-Version"); 
            if (version is null)
                throw new InvalidDataException("Mandatory header Sec-WebSocket-Version missing");
            if (version != "13")
                throw new InvalidDataException("Unsupported version");

            var subProtocols = webRequest.Headers.SingleCommaValues("Sec-WebSocket-Protocol");
            var subProtocol = NegotiateSubProtocols(subProtocols);
            var responseKey = CreateResponseKey(requestKey);

            return BuildHandshakeResponse(responseKey, subProtocol);
        }

        private byte[] BuildHandshakeResponse(string responseKey, string? subProtocol)
        {
            var webResponse = new WebResponse(
                "HTTP/1.1",
                101,
                "Switching Protocols",
                new Dictionary<string, IList<string>>
                {
                    {"Upgrade", new List<string> { "websocket" }},
                    {"Connection", new List<string> { "upgrade" }},
                    {"Sec-WebSocket-Accept", new List<string> { responseKey }},
                },
                null
            );
            if (subProtocol is not null)
                webResponse.Headers.Add("Sec-WebSocket-Protocol", new List<string>{ subProtocol });

            return webResponse.ToBytes();
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

        private byte[] BuildErrorResponse(string reason)
        {
            var webResponse = new WebResponse(
                "HTTP/1.1",
                400,
                "Bad Request",
                new Dictionary<string, IList<string>>
                {
                    { "Connection", new List<string> { "close" }},
                    { "Content-Type", new List<string> { "text/plain; charset=utf-8" }},
                },
                Encoding.UTF8.GetBytes(reason)
            );
            return webResponse.ToBytes();
        }
    }
}