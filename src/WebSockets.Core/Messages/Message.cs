using System;

namespace WebSockets.Core.Messages
{
    /// <summary>
    /// The base class for all WebSocket messages.
    /// </summary>
    public abstract class Message : IEquatable<Message>
    {
        /// <summary>
        /// Construct a message.
        /// </summary>
        /// <param name="type">The message type.</param>
        protected Message(MessageType type)
        {
            Type = type;
        }

        /// <summary>
        /// The message type.
        /// </summary>
        /// <value>The type of the message.</value>
        public MessageType Type { get; private set; }

        /// <inheritdoc />
        public bool Equals(Message? other)
        {
            return other is not null && Type == other.Type;
        }
    }
}