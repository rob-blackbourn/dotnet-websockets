namespace WebSockets.Core
{
    /// <summary>
    /// The type of the frame.
    /// </summary>
    internal enum OpCode : byte
    {
        Continuation = 0,
        Text = 1,
        Binary = 2,
        Close = 8,
        Ping = 9,
        Pong = 10
    }
}

