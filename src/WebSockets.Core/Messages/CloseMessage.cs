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