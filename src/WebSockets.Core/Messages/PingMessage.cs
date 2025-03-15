namespace WebSockets.Core.Messages
{
    /// <summary>
    /// A ping message.
    /// </summary>
    public class PingMessage : DataMessage
    {
        /// <summary>
        /// Construct a ping message.
        /// </summary>
        /// <param name="data">The data that the pong message should return.</param>
        public PingMessage(byte[] data)
            : base(MessageType.Ping, data)
        {
        }
    }
}