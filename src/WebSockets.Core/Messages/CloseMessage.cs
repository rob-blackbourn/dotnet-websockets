using System;
using System.Collections.Generic;

namespace WebSockets.Core.Messages
{
    /// <summary>
    /// A message indicating the connection should be closed.
    /// 
    /// If this is an initiating message the other side will
    /// respond with a close with the same code and reason.
    /// </summary>
    public class CloseMessage : Message, IEquatable<CloseMessage>
    {
        public static Dictionary<int, string> CloseCodes = new()
        {
            {1000, "Normal closure"},
            {1001,  "Going away (e.g. browser tab closed)."},
            {1002,  "Protocol error."},
            {1003,  "Unsupported data (e.g. endpoint only understands text but received binary)."},
            {1004,  "Reserved for future usage"},
            {1005,  "No code received."},
            {1006,  "Connection closed abnormally (closing handshake did not occur)."},
            {1007,  "Invalid payload data (e.g. non UTF-8 data in a text message)."},
            {1008,  "Policy violated."},
            {1009,  "Message too big."},
            {1010,  "Unsupported extension. The client should write the extensions it expected the server to support in the payload."},
            {1011,  "Internal server error."},
            {1015,  "TLS handshake failure."},
        };

        /// <summary>
        /// Construct a close message.
        /// 
        /// If a reason is given a code must be specified.
        /// </summary>
        /// <param name="code">An optional code.</param>
        /// <param name="reason">An optional reason.</param>
        public CloseMessage(ushort? code, string? reason)
            : base(MessageType.Close)
        {
            if (code is null && reason is not null)
                throw new ArgumentOutOfRangeException("If a reason is specified a code must also be");

            Code = code;
            Reason = reason;
        }

        /// <summary>
        /// A code indicating the reason for the close.
        /// </summary>
        /// <value>The code.</value>
        public ushort? Code { get; private set; }
        /// <summary>
        /// A reason for the close.
        /// </summary>
        /// <value>The reason.</value>
        public string? Reason { get; private set; }

        /// <inheritdoc />
        public bool Equals(CloseMessage? other)
        {
            return base.Equals(other) &&
                ((Code == null && other.Code == null) || (Code == other.Code)) &&
                ((Reason == null && other.Reason == null) || (Reason == other.Reason));
        }
    }
}