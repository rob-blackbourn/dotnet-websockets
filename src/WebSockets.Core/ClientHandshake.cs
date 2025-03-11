using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WebSockets.Core
{
    /// <summary>
    /// The client side of the WebSocket handshake.
    /// </summary>
    /// <example>
    /// <code>
    /// var endpoint = IPEndPoint.Parse("localhost:8081");
    /// var tcpClient = new TcpClient();
    /// tcpClient.Connect(endpoint);
    ///
    /// stream = tcpClient.GetStream();
    /// handshake = new ClientHandshake("http://client.com", []);
    /// handshake.WriteRequest("/chat", "www.example.com");
    /// 
    /// // Send the request.
    /// var buffer = new byte[1024];
    /// var isDone = false;
    /// while (!isDone)
    /// {
    ///     var bytesRead = 0L;
    ///     handshake.ReadData(buffer, ref bytesRead, buffer.LongLength);
    ///     if (bytesRead == 0)
    ///         isDone = true;
    ///     else
    ///         stream.Write(buffer, 0, (int)bytesRead);
    /// }
    /// 
    /// // Read the response.
    /// var offset = 0L;
    /// isDone = false;
    /// while (!isDone)
    /// {
    ///     var bytesRead = stream.Read(buffer);
    ///     handshake.WriteData(buffer, offset, bytesRead);
    ///     if (offset == bytesRead)
    ///         offset = 0;
    ///     isDone = handshake.ReadResponse() is not null;
    /// }
    /// 
    /// var webResponse = handshake.ReadResponse();
    /// </code>
    /// </example>
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
        /// 
        /// This method is provided to allow mock implementations for testing.
        /// </summary>
        /// <param name="origin">The origin is the url of the initiator of the request.</param>
        /// <param name="subProtocols">A (possibly empty) array of requested sub-protocols.</param>
        /// <param name="dateTimeProvider">An object which provides the current time.</param>
        /// <param name="nonceGenerator">An object providing secrets.</param>
        internal ClientHandshake(
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