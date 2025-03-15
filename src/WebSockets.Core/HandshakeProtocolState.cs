namespace WebSockets.Core
{
    /// <summary>
    /// The state of the handshake.
    /// </summary>
    public enum HandshakeProtocolState
    {
        Pending,
        Succeeded,
        Failed
    }
}