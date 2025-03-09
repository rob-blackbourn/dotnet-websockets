using System;
using System.Collections.Generic;

namespace WebSockets.Core.Messages
{
    /// <summary>
    /// A WebSocket message.
    /// </summary>
    public abstract class Message : IEquatable<Message>
    {
        protected Message(MessageType type)
        {
            Type = type;
        }

        /// <summary>
        /// The message type.
        /// </summary>
        /// <value>The type of the message.</value>
        public MessageType Type { get; private set; }

        /// <summary>
        /// Serialize the message to bytes.
        /// </summary>
        /// <param name="isClient">If true this is a client message, otherwise it is a server message.</param>
        /// <param name="reserved">The reserved bits.</param>
        /// <param name="maxFrameSize">The maximum size of a frame.</param>
        /// <param name="nonceGenerator">A generator for client masks.</param>
        /// <returns>The message, serialized to bytes.</returns>
        public byte[] Serialize(bool isClient, Reserved? reserved = null, long maxFrameSize = long.MaxValue, INonceGenerator? nonceGenerator = null)
        {
            var writer = new MessageWriter(nonceGenerator ?? new NonceGenerator());
            writer.WriteMessage(this, isClient, reserved ?? Reserved.AllFalse, maxFrameSize);
            var buffers = new List<ArrayBuffer<byte>>();
            while (!writer.IsEmpty)
            {
                var buf = new byte[1024];
                var offset = 0L;
                writer.ReadData(buf, ref offset, buf.LongLength);
                buffers.Add(new ArrayBuffer<byte>(buf, 0, offset));
            }
            return buffers.ToFlatArray();
        }

        /// <summary>
        /// Deserialize data into a message.
        /// </summary>
        /// <param name="data">The data to deserialize.</param>
        /// <returns>The deserialized message.</returns>
        public static Message Deserialize(byte[] data)
        {
            var reader = new MessageReader();
            reader.WriteData(data, 0, data.Length);
            var message = reader.ReadMessage();
            if (message == null)
                throw new InvalidOperationException("failed to deserialize message");
            return message;
        }

        /// <summary>
        /// Check for equality.
        /// </summary>
        /// <param name="other">The other message.</param>
        /// <returns>True if the messages are the same.</returns>
        public bool Equals(Message? other)
        {
            return other is not null && Type == other.Type;
        }
    }
}