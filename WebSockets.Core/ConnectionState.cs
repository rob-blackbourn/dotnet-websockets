namespace WebSockets.Core
{
    public enum HandshakeState
    {
        Pending,
        Succeeded,
        Failed
    }

    public enum ConnectionState
    {
        Connected,
        Closing,
        Closed,
        Faulted
    }
}