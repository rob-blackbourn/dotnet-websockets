using System;
using System.Collections.Generic;

namespace WebSockets.Core.Messages
{
    public abstract class DataMessage : Message, IEquatable<DataMessage>
    {
        public DataMessage(MessageType type, byte[] data)
            : base(type)
        {
            Data = data;
        }

        public byte[] Data { get; private set; }

        public bool Equals(DataMessage? other)
        {
            return base.Equals(other) && Data.Equals(other.Data);
        }
    }
}