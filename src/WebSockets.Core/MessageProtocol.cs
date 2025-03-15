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
        private protected readonly MessageReader _reader = new();
        private protected readonly MessageWriter _writer;

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
            IsClient = isClient;

            _writer = new MessageWriter(nonceGenerator);
        }

        public bool IsClient { get; }

        /// <summary>
        /// The state of the protocol.
        /// </summary>
        /// <value>The protocol state.</value>
        public MessageProtocolState State { get; protected set; } = MessageProtocolState.Connected;

        public bool HasMessage => _reader.HasMessage;
        public bool HasData => _writer.HasData;

        public long ReadData(byte[] destination, long offset, long length)
        {
            return _writer.ReadData(destination, offset, length);
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
                case MessageProtocolState.Connected:
                case MessageProtocolState.Closing:
                    _reader.WriteData(source, offset, length);
                    break;
                case MessageProtocolState.Closed:
                    throw new InvalidOperationException("cannot receive data when closed");
                case MessageProtocolState.Faulted:
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
            var message = _reader.ReadMessage();

            if (message.Type == MessageType.Close)
            {
                if (State == MessageProtocolState.Connected)
                {
                    State = MessageProtocolState.Closing;
                }
                else if (State == MessageProtocolState.Closing)
                {
                    State = MessageProtocolState.Closed;
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
                case MessageProtocolState.Connected:

                    _writer.WriteMessage(message, IsClient, Reserved.AllFalse);

                    if (message.Type == MessageType.Close)
                        State = MessageProtocolState.Closing;

                    break;

                case MessageProtocolState.Closing:

                    if (message.Type != MessageType.Close)
                        throw new InvalidOperationException("can only send a close message when closing.");

                    _writer.WriteMessage(message, IsClient, Reserved.AllFalse);

                    State = MessageProtocolState.Closed;

                    break;

                case MessageProtocolState.Closed:
                case MessageProtocolState.Faulted:

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