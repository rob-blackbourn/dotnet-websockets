using System;
using System.Security.Cryptography;
using System.Text;

namespace WebSockets.Core
{
    /// <summary>
    /// The base protocol class providing functionality shared by both clients
    /// and servers.
    /// </summary>
    public abstract class Handshake
    {
        private protected static byte[] HTTP_EOM = "\r\n\r\n"u8.ToArray();
        private protected const string WebSocketResponseGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private protected readonly FragmentBuffer<byte> _buffer = new FragmentBuffer<byte>();
        private protected readonly string[] _subProtocols;
        private protected readonly IDateTimeProvider _dateTimeProvider;

        /// <summary>
        /// Construct the protocol.
        /// </summary>
        /// <param name="isClient">If true the protocol is for a client; otherwise it is for a server.</param>
        /// <param name="subProtocols">The supported sub-protocols.</param>
        /// <param name="dateTimeProvider">A date/time provider.</param>
        /// <param name="nonceGenerator">A generator for secrets.</param>
        protected Handshake(
            bool isClient,
            string[] subProtocols,
            IDateTimeProvider dateTimeProvider)
        {
            _subProtocols = subProtocols;
            _dateTimeProvider = dateTimeProvider;
        }

        /// <summary>
        /// The state of the connection.
        /// </summary>
        /// <value>The connection state.</value>
        public HandshakeState State { get; protected set; } = HandshakeState.Pending;

        /// <summary>
        /// The sub-protocol negotiated during the handshake.
        /// </summary>
        /// <value>The (possibly null) selected sub-protocol.</value>
        public string? SelectedSubProtocol { get; protected set; } = null;

        /// <summary>
        /// Read handshake data from the provided array into the handshake buffer.
        /// </summary>
        /// <param name="source">The buffer containing the data.</param>
        /// <param name="offset">The offset into the buffer.</param>
        /// <param name="length">The length of the data.</param>
        public void ReadData(byte[] source, ref long offset, long length)
        {
            // TODO: Doesn't support offset and length.
            offset = _buffer.Read(source);
            // TODO: Doesn't return bool.
        }

        /// <summary>
        /// Write data from the handshake buffer into the provided array.
        /// </summary>
        /// <param name="destination">The array to receive the data.</param>
        /// <param name="offset">The point in the buffer to start writing the data.</param>
        /// <param name="length">The length of the buffer.</param>
        public void WriteData(byte[] destination, long offset, long length)
        {
            _buffer.Write(destination, offset, length);
            return;
        }

        private protected static string CreateResponseKey(string requestKey)
        {
            var nonce = SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(requestKey + WebSocketResponseGuid));
            return Convert.ToBase64String(nonce);
        }
    }
}