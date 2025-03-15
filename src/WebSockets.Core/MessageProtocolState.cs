namespace WebSockets.Core
{
    /// <summary>
    /// The state of the protocol.
    /// </summary>
    public enum MessageProtocolState
    {
        Connected,
        Closing,
        Closed,
        Faulted
    }
}