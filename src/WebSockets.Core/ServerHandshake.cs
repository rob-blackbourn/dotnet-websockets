using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WebSockets.Core
{
    /// <summary>
    /// The server side of the WebSocket server handshake.
    /// </summary>
    public class ServerHandshake : Handshake
    {
        /// <summary>
        /// Construct a server handshake object.
        /// </summary>
        /// <param name="subProtocols">The supported sub-protocols.</param>
        public ServerHandshake(string[] subProtocols)
            : this(subProtocols, new DateTimeProvider())
        {
        }

        /// <summary>
        /// Construct a server handshake object.
        /// 
        /// This method is provided to allow mock generators for testing.
        /// </summary>
        /// <param name="subProtocols">The supported sub-protocols.</param>
        /// <param name="acceptPredicate">The supported sub-protocols.</param>
        /// <param name="dateTimeProvider">An implementation of a <see cref="IDateTimeProvider"/>.</param>
        public ServerHandshake(
            string[] subProtocols,
            IDateTimeProvider dateTimeProvider)
            : base(false, subProtocols, dateTimeProvider)
        {
        }

        /// <summary>
        /// Read a <see cref="WebRequest"/> from the protocol.
        /// 
        /// The request will be available when all the bytes of the request bytes have been received.
        /// </summary>
        /// <returns>A <see cref="WebRequest"/> if the complete message has been received; otherwise null.</returns>
        public WebRequest? ReadRequest()
        {
            if (!_buffer.EndsWith(HTTP_EOM))
                return null;

            var text = Encoding.UTF8.GetString(_buffer.ToArray());
            var webRequest = WebRequest.Parse(text);

            return webRequest;
        }

        public WebResponse CreateWebResponse(WebRequest webRequest)
        {
            try
            {
                var (responseKey, subProtocol) = ProcessRequest(webRequest);
                SelectedSubProtocol = subProtocol;
                return WebResponse.CreateAcceptResponse(responseKey, subProtocol);
            }
            catch (InvalidDataException error)
            {
                return WebResponse.CreateErrorResponse(error.Message, _dateTimeProvider);
            }
        }

        public void WriteResponse(WebResponse webResponse)
        {
            var data = webResponse.ToBytes();
            WriteData(data, 0, data.Length);
            State = webResponse.Code == 101 ? HandshakeState.Succeeded : HandshakeState.Failed;
        }

        private (string responseKey, string? subProtocol) ProcessRequest(WebRequest webRequest)
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

            return (responseKey, subProtocol);
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
    }
}