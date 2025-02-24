using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSockets.Core
{
    public class MessageReader
    {
        private readonly FrameReader _frameReader = new FrameReader();
        private readonly Queue<Frame> _frameBuffer = new Queue<Frame>();

        public void Receive(byte[] data, long offset, long length)
        {
            _frameReader.Receive(data, offset, length);
        }

        public Message? Process()
        {
            while (true)
            {
                var frame = _frameReader.Process();
                if (frame == null)
                    return null;

                _frameBuffer.Enqueue(frame);

                if (frame.IsFinal)
                {
                    var message = CreateMessage(_frameBuffer.ToArray());
                    _frameBuffer.Clear();
                    return message;
                }
            }
        }

        Message CreateMessage(Frame[] frames)
        {
            if (frames.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(frames));

            if (frames.Length == 1)
                return CreateMessage(frames[0]);

            if (frames[0].OpCode == OpCode.Continuation)
                throw new InvalidOperationException("first op code cannot be continuation");
            if (!frames.Last().IsFinal)
                throw new InvalidOperationException("the last frame must be final");
            if (frames.Length > 1)
            {
                if (frames.Skip(1).Any(x => x.OpCode != OpCode.Continuation))
                    throw new InvalidOperationException("following frames must be continuations");
                if (frames.Take(frames.Length - 1).Any(x => x.IsFinal))
                    throw new InvalidOperationException("only the last frame can be final");
            }

            var length = frames.Sum(x => x.Payload.Count);
            var buf = new byte[length];
            var offset = 0L;
            foreach (var frame in frames)
            {
                Array.Copy(frame.Payload.Array, frame.Payload.Offset, buf, offset, frame.Payload.Count);
                offset += frame.Payload.Count;
            }
            return CreateMessage(frames[0].OpCode, buf);
        }

        Message CreateMessage(Frame frame)
        {
            return CreateMessage(frame.OpCode, frame.Payload);
        }

        Message CreateMessage(OpCode opCode, ArrayBuffer<byte> payload)
        {
            switch (opCode)
            {
                case OpCode.Text:
                    return new TextMessage(Encoding.UTF8.GetString(payload.ToArray()));
                    
                case OpCode.Binary:
                    return new BinaryMessage(payload.ToArray());

                case OpCode.Close:
                {
                    ushort? code = null;
                    if (payload.Count == 1 || payload.Count > 125)
                        throw new InvalidOperationException("Invalid close payload");
                    else if (payload.Count >= 2)
                        code = BinaryPrimitives.ReadUInt16BigEndian(payload.Slice(0, 2).ToArray());

                    var reason = payload.Count <= 2 ? null : Encoding.UTF8.GetString(payload.Slice(2).ToArray());

                    return new CloseMessage(code, reason);
                }
                case OpCode.Ping:
                    return new PingMessage(payload.ToArray());

                case OpCode.Pong:
                    return new PongMessage(payload.ToArray());

                default:
                    throw new InvalidOperationException("Invalid op code");
            }
        }


    }
}