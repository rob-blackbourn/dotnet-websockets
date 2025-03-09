using System;
using System.Security.Cryptography;
using System.Text;

namespace WebSockets.Core
{
    /// <summary>
    /// The base protocol class providing functionality shared by both clients
    /// and servers.
    /// </summary>
    public abstract class Protocol
    {
        private protected static byte[] HTTP_EOM = "\r\n\r\n"u8.ToArray();
        private protected const string WebSocketResponseGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private protected readonly FragmentBuffer<byte> _handshakeBuffer = new FragmentBuffer<byte>();
        private protected readonly MessageReader _messageReader = new MessageReader();
        private protected readonly MessageWriter _messageWriter;
        private protected readonly string[] _subProtocols;
        private protected readonly IDateTimeProvider _dateTimeProvider;
        private protected bool _isClient;

        /// <summary>
        /// Construct the protocol.
        /// </summary>
        /// <param name="isClient">If true the protocol is for a client; otherwise it is for a server.</param>
        /// <param name="subProtocols">The supported sub-protocols.</param>
        /// <param name="dateTimeProvider">A date/time provider.</param>
        /// <param name="nonceGenerator">A generator for secrets.</param>
        protected Protocol(
            bool isClient,
            string[] subProtocols,
            IDateTimeProvider dateTimeProvider,
            INonceGenerator nonceGenerator)
        {
            _isClient = isClient;
            _subProtocols = subProtocols;
            _dateTimeProvider = dateTimeProvider;

            _messageWriter = new MessageWriter(nonceGenerator);
        }

        /// <summary>
        /// The state of the connection.
        /// </summary>
        /// <value>The connection state.</value>
        public ConnectionState State { get; protected set; } = ConnectionState.Connected;
        public HandshakeState HandshakeState { get; protected set; } = HandshakeState.Pending;
        public string? SelectedSubProtocol { get; protected set; } = null;

        /// <summary>
        /// Read handshake data from the network into the protocol buffer.
        /// </summary>
        /// <param name="buffer">The buffer containing the data.</param>
        /// <param name="offset">The offset into the buffer.</param>
        /// <param name="length">The length of the data.</param>
        public void ReadHandshakeData(byte[] buffer, ref long offset, long length)
        {
            // TODO: Doesn't support offset and length.
            offset = _handshakeBuffer.Read(buffer);
            // TODO: Doesn't return bool.
        }

        public void WriteHandshakeData(byte[] buffer, long offset, long length)
        {
            _handshakeBuffer.Write(buffer, offset, length);
            return;
        }

        public bool ReadMessageData(byte[] buffer, ref long offset, long length)
        {
            return _messageWriter.ReadMessageData(buffer, ref offset, length);
        }

        public void WriteMessageData(byte[] buffer, long offset, long length)
        {
            if (HandshakeState != HandshakeState.Succeeded)
                throw new InvalidOperationException("cannot receive data before handshake completed");

            switch (State)
            {
                case ConnectionState.Connected:
                case ConnectionState.Closing:
                    _messageReader.WriteMessageData(buffer, offset, length);
                    break;
                case ConnectionState.Closed:
                    throw new InvalidOperationException("cannot receive data when closed");
                case ConnectionState.Faulted:
                    throw new InvalidOperationException("cannot receive data when faulted");
                default:
                    throw new InvalidOperationException("invalid internal state");
            }
        }

        public Message? ReadMessage()
        {
            var message = _messageReader.ReadMessage();
            if (message is null)
                return null;

            if (message.Type == MessageType.Close)
            {
                if (State == ConnectionState.Connected)
                {
                    State = ConnectionState.Closing;
                }
                else if (State == ConnectionState.Closing)
                {
                    State = ConnectionState.Closed;
                }
                else
                {
                    throw new InvalidOperationException("received close message when not connected or closing");
                }
            }

            return message;
        }

        public void WriteMessage(Message message)
        {
            if (HandshakeState != HandshakeState.Succeeded)
                throw new InvalidOperationException("cannot receive data before handshake completed");

            switch (State)
            {
                case ConnectionState.Connected:

                    _messageWriter.WriteMessage(message, _isClient, Reserved.AllFalse);

                    if (message.Type == MessageType.Close)
                        State = ConnectionState.Closing;

                    break;

                case ConnectionState.Closing:

                    if (message.Type != MessageType.Close)
                        throw new InvalidOperationException("can only send a close message when closing.");

                    _messageWriter.WriteMessage(message, _isClient, Reserved.AllFalse);

                    State = ConnectionState.Closed;

                    break;

                case ConnectionState.Closed:
                case ConnectionState.Faulted:

                    throw new InvalidOperationException($"cannot send a message in state {State}.");
            }
        }

        private protected static string CreateResponseKey(string requestKey)
        {
            var nonce = SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(requestKey + WebSocketResponseGuid));
            return Convert.ToBase64String(nonce);
        }
    }
}