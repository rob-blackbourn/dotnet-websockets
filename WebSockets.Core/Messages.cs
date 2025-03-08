using System;
using System.Collections.Generic;

namespace WebSockets.Core
{
    /// <summary>
    /// The types of messages.
    /// </summary>
    public enum MessageType
    {
        Text,
        Binary,
        Ping,
        Pong,
        Close
    }

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
                writer.ReadMessageData(buf, ref offset, buf.LongLength);
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
            reader.WriteMessageData(data, 0, data.Length);
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

    /// <summary>
    /// A message with text data.
    /// </summary>
    public class TextMessage : Message, IEquatable<TextMessage>
    {
        public TextMessage(string text)
            : base(MessageType.Text)
        {
            Text = text;
        }
        public string Text { get; private set; }

        public bool Equals(TextMessage? other)
        {
            return base.Equals(other) && Text == other.Text;
        }
    }

    public abstract class DataMessage : Message, IEquatable<DataMessage>
    {
        public DataMessage(MessageType type, byte[] data)
            : base(type)
        {
            Data = data;
        }

        public byte[] Data { get; private set; }

        public bool Equals(DataMessage? other)
        {
            return base.Equals(other) && Data.Equals(other.Data);
        }
    }

    public class BinaryMessage : DataMessage
    {
        public BinaryMessage(byte[] data)
            : base(MessageType.Binary, data)
        {
        }
    }

    public class PingMessage : DataMessage
    {
        public PingMessage(byte[] data)
            : base(MessageType.Ping, data)
        {
        }
    }

    public class PongMessage : DataMessage
    {
        public PongMessage(byte[] data)
            : base(MessageType.Pong, data)
        {
        }
    }

    public class CloseMessage : Message, IEquatable<CloseMessage>
    {
        public CloseMessage(ushort? code, string? reason)
            : base(MessageType.Close)
        {
            Code = code;
            Reason = reason;
        }

        public ushort? Code { get; private set; }
        public string? Reason { get; private set; }

        public bool Equals(CloseMessage? other)
        {
            return base.Equals(other) &&
                ((Code == null && other.Code == null) || (Code == other.Code)) &&
                ((Reason == null && other.Reason == null) || (Reason == other.Reason));
        }
    }
}