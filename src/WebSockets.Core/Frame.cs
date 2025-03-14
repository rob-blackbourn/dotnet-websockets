using System;
using System.Linq;

namespace WebSockets.Core
{
    internal class Frame : IEquatable<Frame>
    {
        public Frame(OpCode opCode, bool isFinal, Reserved reserved, byte[]? mask, ArrayBuffer<byte> payload)
        {
            if (!(mask == null || mask.Length == 4))
                throw new ArgumentOutOfRangeException(nameof(mask));

            if (!Enum.IsDefined<OpCode>(opCode))
                throw new ArgumentOutOfRangeException(nameof(opCode));

            OpCode = opCode;
            IsFinal = isFinal;
            Reserved = reserved;
            Mask = mask;
            Payload = payload;
        }

        public OpCode OpCode { get; private set; }
        public bool IsFinal { get; private set; }
        public Reserved Reserved { get; private set; }
        public byte[]? Mask { get; private set; }
        public ArrayBuffer<byte> Payload { get; private set; }

        public bool Equals(Frame? other)
        {
            return other != null &&
                OpCode == other.OpCode &&
                IsFinal == other.IsFinal &&
                Reserved.Equals(other.Reserved) &&
                (
                    (Mask == null && other.Mask == null) ||
                    (
                        Mask != null &&
                        other.Mask != null &&
                        Mask.SequenceEqual(other.Mask)
                    )
                ) &&
                Payload == other.Payload;
        }
    }
}

