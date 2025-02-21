using System;

namespace WebSockets.Core
{
    public class Frame
    {
        public Frame(OpCode opCode, bool isFinal, bool isRsv1, bool isRsv2, bool isRsv3, byte[]? mask, ArrayBuffer<byte> payload)
        {
            if (!(mask == null || mask.Length == 4))
                throw new ArgumentOutOfRangeException(nameof(mask));

            if (!Enum.IsDefined<OpCode>(opCode))
                throw new ArgumentOutOfRangeException(nameof(opCode));

            OpCode = opCode;
            IsFinal = isFinal;
            IsRsv1 = isRsv1;
            IsRsv2 = isRsv2;
            IsRsv3 = isRsv3;
            Mask = mask;
            Payload = payload;
        }

        public OpCode OpCode { get; private set; }
        public bool IsFinal { get; private set; }
        public bool IsRsv1 { get; private set; }
        public bool IsRsv2 { get; private set; }
        public bool IsRsv3 { get; private set; }
        public byte[]? Mask { get; private set; }
        public ArrayBuffer<byte> Payload { get; private set; }
    }
}

