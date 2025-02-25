using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
    public class ServerProtocol
    {
        enum State
        {
            Handshake,
            Connected,
            Closing,
            Closed,
            Faulted
        }

        private static byte[] HTTP_EOM = "\r\n\r\n"u8.ToArray();
        private const string WebSocketResponseGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        private readonly FragmentBuffer<byte> _handshakeReadBuffer = new FragmentBuffer<byte>();
        private readonly FragmentBuffer<byte> _handshakeWriteBuffer = new FragmentBuffer<byte>();
        private readonly MessageReader _messageReader = new MessageReader();
        private readonly MessageWriter _messageWriter;
        private readonly string[] _supportedSubProtocols;
        private readonly IDateTimeProvider _dateTimeProvider;
        private State _state = State.Handshake;

        public ServerProtocol(
            string[]? supportedSubProtocols = null,
            IDateTimeProvider? dateTimeProvider = null,
            INonceGenerator? nonceGenerator = null)
        {
            _supportedSubProtocols = supportedSubProtocols ?? [];
            _dateTimeProvider = dateTimeProvider ?? new DateTimeProvider();

            _messageWriter = new MessageWriter(nonceGenerator ?? new NonceGenerator());
        }

        public bool IsOpen => _state == State.Connected;
        public bool IsWriteable => IsOpen && !(_handshakeWriteBuffer.Count == 0 && _messageWriter.IsEmpty);

        public void SubmitMessage(Message message)
        {
            switch (_state)
            {
                case State.Handshake:
                    throw new InvalidOperationException("cannot send a message before the handshake is complete.");
                case State.Connected:
                    _messageWriter.SubmitMessage(message, false, Reserved.AllFalse);
                    break;
                case State.Closing:
                    if (message.Type != MessageType.Close)
                        throw new InvalidOperationException("can only send a close message when closing.");
                    _messageWriter.SubmitMessage(message, false, Reserved.AllFalse);
                    break;
                case State.Closed:
                    break;
                case State.Faulted:
                    break;
            }
        }

        public bool Serialize(byte[] buffer, ref long offset)
        {
            if (_handshakeWriteBuffer.Count > 0)
            {
                offset = _handshakeWriteBuffer.Read(buffer);
                return true;
            }
            else
            {
                return _messageWriter.Serialize(buffer, ref offset);
            }
        }

        public void SubmitData(byte[] buffer, long offset, long length)
        {
            switch (_state)
            {
                case State.Handshake:
                    _handshakeReadBuffer.Write(buffer, offset, length);
                    break;
                case State.Connected:
                case State.Closing:
                    _messageReader.SubmitData(buffer, offset, length);
                    break;
                case State.Closed:
                    throw new InvalidOperationException("cannot receive data when closed");
                case State.Faulted:
                    throw new InvalidOperationException("cannot receive data when faulted");
                default:
                    throw new InvalidOperationException("invalid internal state");
            }
        }

        public bool Handshake()
        {
            if (!_handshakeReadBuffer.EndsWith(HTTP_EOM))
                return false;

            try
            {
                var text = Encoding.UTF8.GetString(_handshakeReadBuffer.ToArray());
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
                _state = State.Connected;
                return true;

            }
            catch (InvalidDataException error)
            {
                SendBadRequest(error.Message);
                _state = State.Faulted;
                return true;
            }
        }

        public Message? Deserialize()
        {
            var message = _messageReader.Deserialize();
            if (message is null)
                return null;

            if (message.Type == MessageType.Close)
            {
                if (_state == State.Connected)
                {
                    _state = State.Closing;
                }
                else if (_state == State.Closing)
                {
                    _state = State.Closed;
                }
                else
                {
                    throw new InvalidOperationException("received close message when not connected or closing");
                }
            }

            return message;
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

            _handshakeWriteBuffer.Write(Encoding.ASCII.GetBytes(builder.ToString()));
        }

        private static string CreateResponseKey(string requestKey)
        {
            var nonce = SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(requestKey + WebSocketResponseGuid));
            return Convert.ToBase64String(nonce);
        }

        private string? NegotiateSubProtocols(string[]? candidateSubProtocols)
        {
            if (_supportedSubProtocols.Length == 0 || candidateSubProtocols is null || candidateSubProtocols.Length == 0)
                return null;

            var matches = candidateSubProtocols.Intersect(_supportedSubProtocols).ToList();
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
            _handshakeWriteBuffer.Write(data);
        }
    }
}