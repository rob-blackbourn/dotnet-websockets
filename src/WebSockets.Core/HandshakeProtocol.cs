using System;
using System.Security.Cryptography;
using System.Text;

namespace WebSockets.Core
{
    /// <summary>
    /// The base handshake protocol class providing functionality shared by both
    /// clients and servers.
    /// </summary>
    public abstract class HandshakeProtocol
    {
        internal static byte[] HTTP_EOM = "\r\n\r\n"u8.ToArray();
        private protected const string WebSocketResponseGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private protected readonly FragmentBuffer<byte> _buffer = new();
        private protected readonly string[] _subProtocols;
        private protected readonly IDateTimeProvider _dateTimeProvider;

        /// <summary>
        /// Construct the protocol.
        /// </summary>
        /// <param name="isClient">If true the protocol is for a client; otherwise it is for a server.</param>
        /// <param name="subProtocols">The supported sub-protocols.</param>
        /// <param name="dateTimeProvider">A date/time provider.</param>
        /// <param name="nonceGenerator">A generator for secrets.</param>
        protected private HandshakeProtocol(
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
        public HandshakeProtocolState State { get; protected set; } = HandshakeProtocolState.Pending;

        public bool HasData => !_buffer.IsEmpty;

        /// <summary>
        /// The sub-protocol negotiated during the handshake.
        /// </summary>
        /// <value>The (possibly null) selected sub-protocol.</value>
        public string? SelectedSubProtocol { get; protected set; } = null;

        /// <summary>
        /// Read handshake data from the handshake buffer into the array.
        /// </summary>
        /// <param name="destination">The buffer containing the data.</param>
        /// <param name="offset">The offset into the buffer.</param>
        /// <param name="length">The length of the data.</param>
        public long ReadData(byte[] destination, long offset, long length)
        {
            return _buffer.Read(destination, offset, length);
        }

        /// <summary>
        /// Read handshake data from the handshake buffer into the array.
        /// </summary>
        /// <param name="destination">The buffer containing the data.</param>
        public long ReadData(byte[] destination)
        {
            return ReadData(destination, 0, destination.LongLength);
        }

        /// <summary>
        /// Write data from the array into the handshake buffer.
        /// </summary>
        /// <param name="source">The array to receive the data.</param>
        /// <param name="offset">The point in the buffer to start writing the data.</param>
        /// <param name="length">The length of the buffer.</param>
        public void WriteData(byte[] source, long offset, long length)
        {
            _buffer.Write(source, offset, length);
        }

        public void WriteData(byte[] source)
        {
            WriteData(source, 0, source.LongLength);
        }

        private protected static string CreateResponseKey(string requestKey)
        {
            var nonce = SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(requestKey + WebSocketResponseGuid));
            return Convert.ToBase64String(nonce);
        }
    }
}