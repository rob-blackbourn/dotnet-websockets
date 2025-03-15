using System;
using System.Security.Cryptography;
using System.Text;

using WebSockets.Core.Messages;

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

        public MessageProtocol(bool isClient)
            : this(isClient, new NonceGenerator())
        {
        }

        /// <summary>
        /// Construct the protocol.
        /// </summary>
        /// <param name="isClient">If true the protocol is for a client; otherwise it is for a server.</param>
        /// <param name="nonceGenerator">A generator for secrets.</param>
        internal MessageProtocol(
            bool isClient,
            INonceGenerator nonceGenerator)
        {
            _isClient = isClient;

            _messageWriter = new MessageWriter(nonceGenerator);
        }

        /// <summary>
        /// The state of the protocol.
        /// </summary>
        /// <value>The protocol state.</value>
        public ProtocolState State { get; protected set; } = ProtocolState.Connected;

        public bool HasMessage => _messageReader.HasMessage;
        public bool HasData => _messageWriter.HasData;

        public long ReadData(byte[] destination, long offset, long length)
        {
            return _messageWriter.ReadData(destination, offset, length);
        }

        /// <summary>
        /// Write data to the protocol.
        /// </summary>
        /// <param name="source">The data to write.</param>
        /// <param name="offset">The offset from which the data should be written.</param>
        /// <param name="length">The available length of the data.</param>
        public void WriteData(byte[] source, long offset, long length)
        {
            switch (State)
            {
                case ProtocolState.Connected:
                case ProtocolState.Closing:
                    _messageReader.WriteData(source, offset, length);
                    break;
                case ProtocolState.Closed:
                    throw new InvalidOperationException("cannot receive data when closed");
                case ProtocolState.Faulted:
                    throw new InvalidOperationException("cannot receive data when faulted");
                default:
                    throw new InvalidOperationException("invalid internal state");
            }
        }

        /// <summary>
        /// Read a message from the protocol.
        /// </summary>
        /// <returns>If there is a complete message the message is returned, otherwise null.</returns>
        public Message ReadMessage()
        {
            var message = _messageReader.ReadMessage();

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

        /// <summary>
        /// Write a message to the protocol.
        /// </summary>
        /// <param name="message">The message to write.</param>
        public void WriteMessage(Message message)
        {
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