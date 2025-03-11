namespace WebSockets.Core
{
    /// <summary>
    /// The state of the handshake.
    /// </summary>
    public enum HandshakeState
    {
        Pending,
        Succeeded,
        Failed
    }
}