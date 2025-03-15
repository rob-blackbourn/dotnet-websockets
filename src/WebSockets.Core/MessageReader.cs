using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using WebSockets.Core.Messages;

namespace WebSockets.Core
{
    /// <summary>
    /// A class to read WebSocket messages.
    /// 
    /// Data is submitted (<see cref="WriteData"/>) to the reader, then
    /// messages are produced when processed (<see cref="ReadMessage"/>).
    /// A message may consist of multiple frames.
    /// </summary>
    internal class MessageReader
    {
        private readonly FrameReader _frameReader = new();
        private readonly List<Frame> _frameBuffer = new();
        private readonly Queue<Message> _messages = new();

        /// <summary>
        /// A property to indicate if the reader requires more data to
        /// produce the current message.
        /// </summary>
        /// <returns>True if there is more data is required; otherwise false.</returns>
        public bool NeedsData => _frameReader.NeedsData || _frameBuffer.Count > 0;

        public bool HasMessage => _messages.Count > 0;

        /// <summary>
        /// Submit data to be deserialized to messages.
        /// 
        /// After submitting the data <see cref="ReadMessage"/> must be called to
        /// generate the messages.
        /// </summary>
        /// <param name="source">The data to create the messages with.</param>
        /// <param name="offset"><The point to start reading the buffer./param>
        /// <param name="length">The length of the buffer to read.</param>
        public void WriteData(byte[] source, long offset, long length)
        {
            _frameReader.WriteData(source, offset, length);
            ProcessDataUntilStalled();
        }

        /// <summary>
        /// Process submitted data to produce messages.
        /// </summary>
        /// <returns>A message, if there is sufficient data available.</returns>
        public Message ReadMessage()
        {
            return _messages.Dequeue();
        }

        private void ProcessDataUntilStalled()
        {
            var done = false;
            while (!done)
            {
                if (!_frameReader.HasFrame)
                {
                    done = true;
                    continue;
                }

                var frame = _frameReader.ReadFrame();
                _frameBuffer.Add(frame);
                if (frame.IsFinal)
                {
                    var message = CreateMessage(_frameBuffer);
                    _messages.Enqueue(message);
                    _frameBuffer.Clear();
                }
            }
        }

        /// <summary>
        /// Create a message from an array of frames.
        /// </summary>
        /// <param name="frames">The frames from which a message should be  read.</param>
        /// <returns>The message that was created.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the message cannot be created.
        /// </exception>
        private static Message CreateMessage(List<Frame> frames)
        {
            if (frames.Count == 0)
                throw new ArgumentOutOfRangeException(nameof(frames));

            if (frames.Count == 1)
                return CreateMessage(frames[0]);

            if (frames[0].OpCode == OpCode.Continuation)
                throw new InvalidOperationException("first op code cannot be continuation");
            if (!frames.Last().IsFinal)
                throw new InvalidOperationException("the last frame must be final");
            if (frames.Count > 1)
            {
                if (frames.Skip(1).Any(x => x.OpCode != OpCode.Continuation))
                    throw new InvalidOperationException("following frames must be continuations");
                if (frames.Take(frames.Count - 1).Any(x => x.IsFinal))
                    throw new InvalidOperationException("only the last frame can be final");
            }

            var length = frames.Sum(x => x.Payload.Count);
            var buf = new byte[length];
            var offset = 0L;
            foreach (var frame in frames)
            {
                Array.Copy(frame.Payload.Buffer, frame.Payload.Offset, buf, offset, frame.Payload.Count);
                offset += frame.Payload.Count;
            }
            return CreateMessage(frames[0].OpCode, buf);
        }

        /// <summary>
        /// Create a message from a frame.
        /// </summary>
        /// <param name="frame">The frame to use.</param>
        /// <returns>The created message.</returns>
        private static Message CreateMessage(Frame frame)
        {
            return CreateMessage(frame.OpCode, frame.Payload);
        }

        /// <summary>
        /// Create a message from an opcode and an array of bytes.
        /// </summary>
        /// <param name="opCode">The opcode.</param>
        /// <param name="payload">The array of bytes.</param>
        /// <returns>The created message.</returns>
        private static Message CreateMessage(OpCode opCode, ArrayBuffer<byte> payload)
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