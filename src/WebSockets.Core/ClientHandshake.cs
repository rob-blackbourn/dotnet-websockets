using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WebSockets.Core
{
    /// <summary>
    /// A sans-io implementation of the client side of the WebSocket protocol.
    /// </summary>
    public class ClientHandshake : Handshake
    {
        private readonly string _origin;
        private readonly string _key;

        /// <summary>
        /// Construct a client handshake.
        /// </summary>
        /// <param name="origin">The origin is the url of the initiator of the request.</param>
        /// <param name="subProtocols">A (possibly empty) array of requested sub-protocols.</param>
        public ClientHandshake(string origin, string[] subProtocols)
            : this(origin, subProtocols, new DateTimeProvider(), new NonceGenerator())
        {
        }

        /// <summary>
        /// Construct a client handshake.
        /// </summary>
        /// <param name="origin">The origin is the url of the initiator of the request.</param>
        /// <param name="subProtocols">A (possibly empty) array of requested sub-protocols.</param>
        /// <param name="dateTimeProvider">An object which provides the current time.</param>
        /// <param name="nonceGenerator">An object providing secrets.</param>
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

        /// <summary>
        /// Write a handshake request.
        /// </summary>
        /// <param name="path">The path on the server.</param>
        /// <param name="host">The server name.</param>
        public void WriteRequest(string path, string host)
        {
            var webRequest = WebRequest.CreateUpgradeRequest(path, host, _origin, _key, _subProtocols);
            var data = Encoding.ASCII.GetBytes(webRequest.ToString());
            WriteData(data, 0, data.LongLength);
        }

        /// <summary>
        /// Read a handshake response.
        /// </summary>
        /// <returns>The response from the server, or null if a complete response has yet to be received.</returns>
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