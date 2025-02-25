using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WebSockets.Core
{
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

        private readonly FragmentBuffer<byte> _handshakeBuffer = new FragmentBuffer<byte>();
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

        public Queue<byte[]> SendBuffer { get; } = new Queue<byte[]>();
        public Queue<Message> MessagesReceived { get; } = new Queue<Message>();
        public bool IsOpen => _state == State.Connected;

        public bool Send(byte[] buffer, ref long offset)
        {
            return _messageWriter.Write(buffer, ref offset);
        }

        public bool Receive(byte[] buffer, long offset, long length)
        {
            switch (_state)
            {
                case State.Handshake:
                    return ReceiveHandshake(buffer, offset, length);
                case State.Connected:
                case State.Closing:
                    return ReceiveMessages(buffer, offset, length);
                case State.Closed:
                    throw new InvalidOperationException("cannot receive data when closed");
                case State.Faulted:
                    throw new InvalidOperationException("cannot receive data when faulted");
                default:
                    throw new InvalidOperationException("invalid internal state");
            }
        }

        private bool ReceiveHandshake(byte[] buffer, long offset, long length)
        {
            _handshakeBuffer.Write(buffer, offset, length);
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

        private bool ReceiveMessages(byte[] data, long offset, long length)
        {
            _messageReader.Receive(data, offset, length);
            var isDone = false;
            while (!isDone)
            {
                var message = _messageReader.Process();
                if (message is null)
                    isDone = true;
                else
                    HandleMessage(message);
            }

            return MessagesReceived.Count > 0;
        }

        private void HandleMessage(Message message)
        {
            MessagesReceived.Enqueue(message);

            if (message.Type == MessageType.Close)
            {
                var closeMessage = ((CloseMessage)message);
                if (_state == State.Connected)
                {
                    // The client has started the close handshake.
                    // Should respond with a close frame with the same payload.
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
        }

        public void SendMessage(Message message)
        {
            _messageWriter.Send(message, false, Reserved.AllFalse);

            // TODO: Write the message to the send buffer.
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

            SendBuffer.Enqueue(Encoding.ASCII.GetBytes(builder.ToString()));
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
            SendBuffer.Enqueue(data);
        }
    }
}