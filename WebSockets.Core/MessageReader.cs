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

        public void Receive(byte[] data)
        {
            _frameReader.Receive(data);
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

        public Message CreateMessage(Frame[] frames)
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

            var length = frames.Sum(x => x.Payload.Length);
            var buf = new byte[length];
            var offset = 0;
            foreach (var frame in frames)
            {
                Array.Copy(frame.Payload, 0, buf, offset, frame.Payload.Length);
                offset += frame.Payload.Length;
            }
            return CreateMessage(frames[0].OpCode, buf);
        }

        public Message CreateMessage(Frame frame)
        {
            return CreateMessage(frame.OpCode, frame.Payload);
        }

        public Message CreateMessage(OpCode opCode, byte[] payload)
        {
            switch (opCode)
            {
                case OpCode.Text:
                    return new TextMessage(Encoding.UTF8.GetString(payload));
                case OpCode.Binary:
                    return new BinaryMessage(payload);
                case OpCode.Close:
                {
                    ushort? code = null;
                    if (payload.Length == 1 || payload.Length > 125)
                        throw new InvalidOperationException("Invalid close payload");
                    else if (payload.Length >= 2)
                        code = BinaryPrimitives.ReadUInt16BigEndian(payload);

                    var reason = payload.Length <= 2 ? null : Encoding.UTF8.GetString(payload, 2, payload.Length - 2);

                    return new CloseMessage(code, reason);
                }
                case OpCode.Ping:
                    return new PingMessage(payload);
                case OpCode.Pong:
                    return new PongMessage(payload);
                default:
                    throw new InvalidOperationException("Invalid op code");
            }
        }


    }
}