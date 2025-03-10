using System.Collections.Generic;
using System.IO;
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
    public class ClientHandshake : Handshake
    {
        private readonly string _origin;
        private readonly string _key;

        public ClientHandshake(string origin, string[] subProtocols)
            : this(origin, subProtocols, new DateTimeProvider(), new NonceGenerator())
        {
        }

        public ClientHandshake(
            string origin,
            string[] subProtocols,
            IDateTimeProvider dateTimeProvider,
            INonceGenerator nonceGenerator)
            : base(true, subProtocols, dateTimeProvider)
        {
            _origin = origin;
            _key = nonceGenerator.CreateClientKey();
        }

        public void WriteRequest(string path, string host)
        {
            var webRequest = BuildRequest(path, host);
            var data = Encoding.ASCII.GetBytes(webRequest.ToString());
            WriteData(data, 0, data.LongLength);
        }

        public WebResponse? ReadResponse()
        {
            var index = _buffer.IndexOf(HTTP_EOM, 0);
            if (index == -1)
                return null;

            var webResponse = WebResponse.Parse(_buffer.ToArray());
            if (webResponse.Code != 101)
            {
                State = HandshakeState.Failed;
                return webResponse;
            }
            
            SelectedSubProtocol = ProcessResponse(webResponse);
            State = HandshakeState.Succeeded;
            return webResponse;
        }

        private WebRequest BuildRequest(string path, string host)
        {
            var webRequest = new WebRequest(
                "GET",
                path,
                "HTTP/1.1",
                new Dictionary<string, IList<string>>
                {
                    {"Host", new List<string> { host }},
                    {"Upgrade", new List<string> { "websocket" }},
                    {"Connection", new List<string> { "Upgrade" }},
                    {"Origin", new List<string> { _origin }},
                    {"Sec-WebSocket-Version", new List<string> { "13" }},
                    {"Sec-WebSocket-Key", new List<string> { _key }},
                }
            );
            if (_subProtocols is not null && _subProtocols.Length > 0)
                webRequest.Headers.Add("Sec-WebSocket-Protocol", new List<string> { string.Join(',', _subProtocols) });

            return webRequest;
        }

        private string? ProcessResponse(WebResponse webResponse)
        {
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

            return webResponse.Headers.SingleValue("Sec-WebSocket-Protocol");
        }
    }
}