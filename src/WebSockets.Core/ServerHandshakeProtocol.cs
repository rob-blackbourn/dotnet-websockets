using System.IO;
using System.Linq;

using WebSockets.Core.Http;

namespace WebSockets.Core
{
    /// <summary>
    /// The server side of the WebSocket handshake protocol.
    /// </summary>
    public class ServerHandshakeProtocol : HandshakeProtocol
    {
        /// <summary>
        /// Construct a server handshake object.
        /// </summary>
        /// <param name="subProtocols">The supported sub-protocols.</param>
        public ServerHandshakeProtocol(string[] subProtocols)
            : this(subProtocols, new DateTimeProvider())
        {
        }

        internal ServerHandshakeProtocol(
            string[] subProtocols,
            IDateTimeProvider dateTimeProvider)
            : base(subProtocols, dateTimeProvider)
        {
        }

        /// <summary>
        /// Read a <see cref="WebRequest"/> from the protocol.
        /// 
        /// The request will be available when all of the request bytes have been received.
        /// </summary>
        /// <returns>A <see cref="WebRequest"/> if the complete message has been received; otherwise null.</returns>
        public HttpRequest? ReadRequest()
        {
            if (_buffer.IndexOf(HTTP_EOM, 0) == -1)
                return null;

            var webRequest = HttpRequest.Parse(_buffer.ToArray());

            return webRequest;
        }

        /// <summary>
        /// Create the WebSocket response using the web request.
        /// 
        /// If the request is valid an accept/upgrade response is generated.
        /// An invalid request will generate a 400 response containing the
        /// reason for the rejection.
        /// 
        /// An application may inspect the request and create it's own bad
        /// response, for example if the path is not valid.
        /// </summary>
        /// <param name="webRequest">The request from the client.</param>
        /// <returns>The response to be sent to the client.</returns>
        public HttpResponse CreateWebResponse(HttpRequest webRequest)
        {
            try
            {
                var (responseKey, subProtocol) = ProcessRequest(webRequest);
                SelectedSubProtocol = subProtocol;
                return HttpResponse.CreateAcceptResponse(responseKey, subProtocol);
            }
            catch (InvalidDataException error)
            {
                return HttpResponse.CreateErrorResponse(error.Message, _dateTimeProvider.Now);
            }
        }

        /// <summary>
        /// Write a web response to the handshake buffer.
        /// </summary>
        /// <param name="webResponse">The response to send to the client.</param>
        public void WriteResponse(HttpResponse webResponse)
        {
            var data = webResponse.ToBytes();
            WriteData(data);
            State = webResponse.Code == 101
                ? HandshakeProtocolState.Succeeded
                : HandshakeProtocolState.Failed;
        }

        private (string responseKey, string? subProtocol) ProcessRequest(
            HttpRequest webRequest)
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