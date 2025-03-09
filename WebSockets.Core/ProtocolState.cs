namespace WebSockets.Core
{
    public enum HandshakeState
    {
        Pending,
        Succeeded,
        Failed
    }

    public enum ProtocolState
    {
        Connected,
        Closing,
        Closed,
        Faulted
    }
}