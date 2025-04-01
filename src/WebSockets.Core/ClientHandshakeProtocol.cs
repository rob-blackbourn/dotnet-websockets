using System.IO;
using System.Text;

using WebSockets.Core.Http;

namespace WebSockets.Core
{
    /// <summary>
    /// The client side of the WebSocket handshake protocol.
    /// </summary>
    public class ClientHandshakeProtocol : HandshakeProtocol
    {
        private readonly string _key;

        /// <summary>
        /// Construct a client handshake.
        /// </summary>
        /// <param name="origin">The origin is the url of the initiator of the request.</param>
        /// <param name="subProtocols">A (possibly empty) array of requested sub-protocols.</param>
        public ClientHandshakeProtocol(string origin, string[] subProtocols)
            : this(origin, subProtocols, new DateTimeProvider(), new NonceGenerator())
        {
        }

        public string Origin { get; }

        internal ClientHandshakeProtocol(
            string origin,
            string[] subProtocols,
            IDateTimeProvider dateTimeProvider,
            INonceGenerator nonceGenerator)
            : base(subProtocols, dateTimeProvider)
        {
            Origin = origin;
            _key = nonceGenerator.CreateClientKey();
        }

        /// <summary>
        /// Write a handshake request.
        /// </summary>
        /// <param name="path">The path on the server.</param>
        /// <param name="host">The server name.</param>
        public void WriteRequest(string path, string host)
        {
            var webRequest = HttpRequest.CreateUpgradeRequest(path, host, Origin, _key, _subProtocols);
            var data = Encoding.ASCII.GetBytes(webRequest.ToString());
            WriteData(data);
        }

        /// <summary>
        /// Read a handshake response.
        /// </summary>
        /// <returns>The response from the server, or null if a complete response has yet to be received.</returns>
        public HttpResponse? ReadResponse()
        {
            var index = _buffer.IndexOf(HTTP_EOM, 0);
            if (index == -1)
                return null;

            var webResponse = HttpResponse.Parse(_buffer.ToArray());
            if (webResponse.Code != 101)
            {
                State = HandshakeProtocolState.Failed;
                return webResponse;
            }

            SelectedSubProtocol = ProcessResponse(webResponse);
            State = HandshakeProtocolState.Succeeded;
            return webResponse;
        }

        private string? ProcessResponse(HttpResponse webResponse)
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