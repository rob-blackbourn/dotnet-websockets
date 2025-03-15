namespace WebSockets.Core.Messages
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
}