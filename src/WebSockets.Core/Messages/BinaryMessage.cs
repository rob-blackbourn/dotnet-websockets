namespace WebSockets.Core.Messages
{
    /// <summary>
    /// A WebSocket message with a payload of binary data.
    /// </summary>
    public class BinaryMessage : DataMessage
    {
        /// <summary>
        /// Construct a binary message.
        /// </summary>
        /// <param name="data">The message data.</param>
        public BinaryMessage(byte[] data)
            : base(MessageType.Binary, data)
        {
        }
    }
}