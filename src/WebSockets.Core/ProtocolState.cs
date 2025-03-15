namespace WebSockets.Core
{
    /// <summary>
    /// The state of the protocol.
    /// </summary>
    public enum ProtocolState
    {
        Connected,
        Closing,
        Closed,
        Faulted
    }
}