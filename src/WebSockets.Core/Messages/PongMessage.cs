namespace WebSockets.Core.Messages
{
    /// <summary>
    /// The message used to respond to a ping message.
    /// </summary>
    public class PongMessage : DataMessage
    {
        /// <summary>
        /// Construct a pong message.
        /// </summary>
        /// <param name="data">The data sent by the ping messages.</param>
        public PongMessage(byte[] data)
            : base(MessageType.Pong, data)
        {
        }
    }
}