using System;
using System.Security.Cryptography;
using System.Text;

namespace WebSockets.Core
{
    /// <summary>
    /// The base protocol class providing functionality shared by both clients
    /// and servers.
    /// </summary>
    public class MessageProtocol
    {
        private protected const string WebSocketResponseGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private protected readonly MessageReader _messageReader = new MessageReader();
        private protected readonly MessageWriter _messageWriter;
        private protected bool _isClient;

        /// <summary>
        /// Construct the protocol.
        /// </summary>
        /// <param name="isClient">If true the protocol is for a client; otherwise it is for a server.</param>
        /// <param name="subProtocols">The supported sub-protocols.</param>
        /// <param name="dateTimeProvider">A date/time provider.</param>
        /// <param name="nonceGenerator">A generator for secrets.</param>
        public MessageProtocol(
            bool isClient,
            INonceGenerator nonceGenerator)
        {
            _isClient = isClient;

            _messageWriter = new MessageWriter(nonceGenerator);
        }

        /// <summary>
        /// The state of the connection.
        /// </summary>
        /// <value>The connection state.</value>
        public ProtocolState State { get; protected set; } = ProtocolState.Connected;

        public bool ReadData(byte[] buffer, ref long offset, long length)
        {
            return _messageWriter.ReadMessageData(buffer, ref offset, length);
        }

        public void WriteData(byte[] buffer, long offset, long length)
        {
            // if (HandshakeState != HandshakeState.Succeeded)
            //     throw new InvalidOperationException("cannot receive data before handshake completed");

            switch (State)
            {
                case ProtocolState.Connected:
                case ProtocolState.Closing:
                    _messageReader.WriteMessageData(buffer, offset, length);
                    break;
                case ProtocolState.Closed:
                    throw new InvalidOperationException("cannot receive data when closed");
                case ProtocolState.Faulted:
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
                if (State == ProtocolState.Connected)
                {
                    State = ProtocolState.Closing;
                }
                else if (State == ProtocolState.Closing)
                {
                    State = ProtocolState.Closed;
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
            // if (HandshakeState != HandshakeState.Succeeded)
            //     throw new InvalidOperationException("cannot receive data before handshake completed");

            switch (State)
            {
                case ProtocolState.Connected:

                    _messageWriter.WriteMessage(message, _isClient, Reserved.AllFalse);

                    if (message.Type == MessageType.Close)
                        State = ProtocolState.Closing;

                    break;

                case ProtocolState.Closing:

                    if (message.Type != MessageType.Close)
                        throw new InvalidOperationException("can only send a close message when closing.");

                    _messageWriter.WriteMessage(message, _isClient, Reserved.AllFalse);

                    State = ProtocolState.Closed;

                    break;

                case ProtocolState.Closed:
                case ProtocolState.Faulted:

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