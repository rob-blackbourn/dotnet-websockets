using System;
using System.Collections.Generic;

namespace WebSockets.Core.Messages
{
    public class CloseMessage : Message, IEquatable<CloseMessage>
    {
        public CloseMessage(ushort? code, string? reason)
            : base(MessageType.Close)
        {
            Code = code;
            Reason = reason;
        }

        public ushort? Code { get; private set; }
        public string? Reason { get; private set; }

        public bool Equals(CloseMessage? other)
        {
            return base.Equals(other) &&
                ((Code == null && other.Code == null) || (Code == other.Code)) &&
                ((Reason == null && other.Reason == null) || (Reason == other.Reason));
        }
    }
}