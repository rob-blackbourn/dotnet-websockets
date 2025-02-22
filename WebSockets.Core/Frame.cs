using System;
using System.Collections.Generic;

namespace WebSockets.Core
{
    public struct Reserved
    {
        public static Reserved AllFalse {get;} = new Reserved(false, false, false);

        public Reserved(bool isRsv1, bool isRsv2, bool isRsv3)
        {
            IsRsv1 = isRsv1;
            IsRsv2 = isRsv2;
            IsRsv3 = isRsv3;
        }

        public bool IsRsv1 { get; private set; }
        public bool IsRsv2 { get; private set; }
        public bool IsRsv3 { get; private set; }

    }

    public class Frame
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

        public byte[] Serialize()
        {
            var writer = new FrameWriter();
            writer.Frames.Enqueue(this);
            var buffers = new List<ArrayBuffer<byte>>();
            var isDone = false;
            while (!isDone)
            {
                var buf = new byte[1024];
                var offset = 0L;
                isDone = writer.Write(buf, ref offset);
                buffers.Add(new ArrayBuffer<byte>(buf, 0, offset));
            }
            return buffers.ToFlatArray();
        }
    }
}

