using System;

namespace WebSockets.Core.Messages
{
    /// <summary>
    /// The base class for all messages which hold raw data.
    /// </summary>
    public abstract class DataMessage : Message, IEquatable<DataMessage>
    {
        /// <summary>
        /// Construct a data message.
        /// </summary>
        /// <param name="type">The type of the message.</param>
        /// <param name="data">The message data.</param>
        public DataMessage(MessageType type, byte[] data)
            : base(type)
        {
            Data = data;
        }

        /// <summary>
        /// The data associated with the message.
        /// </summary>
        /// <value>The message data.</value>
        public byte[] Data { get; private set; }

        /// <inheritdoc />
        public bool Equals(DataMessage? other)
        {
            return base.Equals(other) && Data.Equals(other.Data);
        }
    }
}