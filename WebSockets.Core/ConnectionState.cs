namespace WebSockets.Core
{
    public enum ConnectionState
    {
        Handshake,
        Connected,
        Closing,
        Closed,
        Faulted
    }
}