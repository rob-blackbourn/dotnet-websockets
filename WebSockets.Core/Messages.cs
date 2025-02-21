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
        public DataMessage(MessageType type, byte[] data)
            :   base(type)
        {
            Data = data;
        }

        public byte[] Data { get; private set; }
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