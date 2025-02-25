using System;
using System.Collections.Generic;

namespace WebSockets.Core
{
    public enum MessageType
    {
        Text,
        Binary,
        Ping,
        Pong,
        Close
    }

    public abstract class Message : IEquatable<Message>
    {
        protected Message(MessageType type)
        {
            Type = type;
        }

        public MessageType Type { get; private set; }

        public byte[] Serialize(bool isClient, Reserved? reserved = null, long maxFrameSize = long.MaxValue, INonceGenerator? nonceGenerator = null)
        {
            var writer = new MessageWriter(nonceGenerator ?? new NonceGenerator());
            writer.Send(this, isClient, reserved ?? Reserved.AllFalse, maxFrameSize);
            var buffers = new List<ArrayBuffer<byte>>();
            var isDone = false;
            while (!isDone)
            {
                var buf = new byte[1024];
                var offset = 0L;
                isDone = writer.Write(buf, ref offset);
                buffers.Add(new ArrayBuffer<byte>(buf, 0, offset));
            }
            return buffers.ToFlatArray();
        }

        public static Message Deserialize(byte[] data)
        {
            var reader = new MessageReader();
            reader.Receive(data, 0, data.Length);
            var message = reader.Process();
            if (message == null)
                throw new InvalidOperationException("failed to deserialize message");
            return message;
        }

        public bool Equals(Message? other)
        {
            return other is not null && Type == other.Type;
        }
    }

    public class TextMessage : Message, IEquatable<TextMessage>
    {
        public TextMessage(string text)
            :   base(MessageType.Text)
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
            :   base(type)
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
            :   base(MessageType.Close)
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