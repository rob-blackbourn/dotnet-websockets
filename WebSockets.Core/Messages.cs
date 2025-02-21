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

    public abstract class Message
    {
        protected Message(MessageType type)
        {
            Type = type;
        }

        public MessageType Type { get; private set; }
    }

    public class TextMessage : Message
    {
        public TextMessage(string text)
            :   base(MessageType.Text)
        {
            Text = text;
        }
        public string Text { get; private set; }
    }

    public abstract class DataMessage : Message
    {
        public DataMessage(MessageType type, ArrayBuffer<byte> data)
            :   base(type)
        {
            Data = data;
        }

        public ArrayBuffer<byte> Data { get; private set; }
    }

    public class BinaryMessage : DataMessage
    {
        public BinaryMessage(ArrayBuffer<byte> data)
            : base(MessageType.Binary, data)
        {
        }
    }

    public class PingMessage : DataMessage
    {
        public PingMessage(ArrayBuffer<byte> data)
            : base(MessageType.Ping, data)
        {
        }
    }

    public class PongMessage : DataMessage
    {
        public PongMessage(ArrayBuffer<byte> data)
            : base(MessageType.Pong, data)
        {
        }
    }

    public class CloseMessage : Message
    {
        public CloseMessage(ushort? code, string? reason)
            :   base(MessageType.Close)
        {
            Code = code;
            Reason = reason;
        }

        public ushort? Code { get; private set; }
        public string? Reason { get; private set; }
    }
}