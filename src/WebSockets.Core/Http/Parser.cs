namespace WebSockets.Core.Http
{
    abstract class Parser
    {
        internal static byte[] EOL = "\r\n"u8.ToArray();
        internal static byte[] EOM = "\r\n\r\n"u8.ToArray();
    }
}
