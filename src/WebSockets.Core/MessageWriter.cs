using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using WebSockets.Core.Messages;

namespace WebSockets.Core
{
    /// <summary>
    /// A class to write WebSocket messages.
    /// 
    /// Messages are submitted (<see cref="WriteMessage"/>) to the writer, and then processed (<see cref="ReadData"/>)
    /// into data buffers until the writer is empty (<see cref="IsEmpty"/>). Note that one message may produce several frames to write.
    /// </summary>
    internal class MessageWriter
    {
        private readonly INonceGenerator _nonceGenerator;
        private readonly FrameWriter _frameWriter = new FrameWriter();

        /// <summary>
        /// Construct a message writer.
        /// 
        /// A factory is provided for the nonce generator to allow mocking during testing.
        /// </summary>
        /// <param name="nonceGenerator">This factory creates the masks used by client frames.</param>
        public MessageWriter(INonceGenerator nonceGenerator)
        {
            _nonceGenerator = nonceGenerator;
        }

        /// <summary>
        /// A property indicating if there is anything to write.
        /// </summary>
        /// <returns>True if there is not data to be sent; otherwise false.</returns>
        //public bool IsEmpty => _frameWriter.IsEmpty;
        public bool HasData => _frameWriter.HasData;

        /// <summary>
        /// Submits a message to the writer.
        /// 
        /// The message must be written with the <see cref="ReadData"/> method.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="isClient">True if the sender is a client; otherwise false for a server.</param>
        /// <param name="reserved">The reserved data.</param>
        /// <param name="maxFrameSize">The maximum size of a frame to write.</param>
        /// <returns>The number of frames being sent.</returns>
        public int WriteMessage(Message message, bool isClient, Reserved reserved, long maxFrameSize = long.MaxValue)
        {
            var opCode = GetOpCode(message.Type);
            var payload = GetPayload(message);

            var frameCount = 0;
            while (payload.Count > 0 || opCode != OpCode.Continuation)
            {
                var length = long.Min(payload.Count, maxFrameSize);
                var framePayload = payload.Slice(0, length);
                payload = payload.Slice(length);
                var isFinal = payload.Count == 0;
                var mask = isClient ? _nonceGenerator.CreateMask() : null;
                var frame = new Frame(opCode, isFinal, reserved, mask, framePayload);
                _frameWriter.WriteFrame(frame);
                frameCount += 1;
                opCode = OpCode.Continuation;
            }

            return frameCount;
        }

        public long ReadData(byte[] destination, long offset, long length)
        {
            return _frameWriter.ReadData(destination, offset, length);
        }

        /// <summary>
        /// Convert the message payload to an array of bytes.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>The message payload as an array of bytes.</returns>
        private ArrayBuffer<byte> GetPayload(Message message)
        {
            switch (message.Type)
            {
                case MessageType.Text:
                    return Encoding.UTF8.GetBytes(((TextMessage)message).Text);
                case MessageType.Binary:
                case MessageType.Ping:
                case MessageType.Pong:
                    return ((DataMessage)message).Data;
                case MessageType.Close:
                    {
                        var closeMessage = (CloseMessage)message;
                        if (!closeMessage.Code.HasValue && closeMessage.Reason != null)
                            throw new InvalidOperationException("a close message with reason text must have a code");

                        var data = new List<byte[]>();

                        if (closeMessage.Code.HasValue)
                        {
                            var codeBuf = new byte[2];
                            BinaryPrimitives.WriteUInt16BigEndian(codeBuf, closeMessage.Code.Value);
                            data.Add(codeBuf);
                        }

                        if (closeMessage.Reason != null)
                        {
                            var reasonBuf = Encoding.UTF8.GetBytes(closeMessage.Reason);
                            if (reasonBuf.Length > 123)
                                throw new InvalidOperationException("close reason must be 123 bytes or less");
                            data.Add(reasonBuf);
                        }

                        return data.SelectMany(x => x).ToArray();
                    }
                default:
                    throw new InvalidOperationException("unhandled message type");
            }
        }

        /// <summary>
        /// Converts the message type to an opcode.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        /// <returns>The frame opcode.</returns>
        private OpCode GetOpCode(MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.Text:
                    return OpCode.Text;
                case MessageType.Binary:
                    return OpCode.Binary;
                case MessageType.Ping:
                    return OpCode.Ping;
                case MessageType.Pong:
                    return OpCode.Pong;
                case MessageType.Close:
                    return OpCode.Close;
                default:
                    throw new ArgumentOutOfRangeException(nameof(messageType));
            }
        }
    }
}