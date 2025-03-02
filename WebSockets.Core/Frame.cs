using System;
using System.Collections.Generic;
using System.Linq;

namespace WebSockets.Core
{
    class Frame : IEquatable<Frame>
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
            writer.WriteFrame(this);
            var buffers = new List<ArrayBuffer<byte>>();
            var isDone = false;
            while (!isDone)
            {
                var buf = new byte[1024];
                var offset = 0L;
                isDone = writer.ReadFrameData(buf, ref offset, buf.LongLength);
                buffers.Add(new ArrayBuffer<byte>(buf, 0, offset));
            }
            return buffers.ToFlatArray();
        }

        public static Frame Deserialize(byte[] data)
        {
            var reader = new FrameReader();
            reader.WriteFrameData(data, 0, data.Length);
            var frame = reader.ReadFrame();
            if (frame == null)
                throw new InvalidOperationException("failed to deserialize");
            return frame;
        }

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

