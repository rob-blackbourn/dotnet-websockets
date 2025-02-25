using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace WebSockets.Core
{
    /// <summary>
    /// A class to write WebSocket messages.
    /// 
    /// Messages are sent as one or more frames of data.
    /// </summary>
    public class MessageWriter
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
        public bool IsEmpty => _frameWriter.IsEmpty;

        /// <summary>
        /// Submits a message to the writer.
        /// 
        /// The message must be written with the <see cref="Process"/> method.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="isClient">True if the sender is a client; otherwise false for a server.</param>
        /// <param name="reserved">The reserved data.</param>
        /// <param name="maxFrameSize">The maximum size of a frame to write.</param>
        /// <returns>The number of frames being sent.</returns>
        public int Submit(Message message, bool isClient, Reserved reserved, long maxFrameSize = long.MaxValue)
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
                var mask = isClient ? _nonceGenerator.Create() : null;
                var frame = new Frame(opCode, isFinal, reserved, mask, framePayload);
                _frameWriter.Submit(frame);
                frameCount += 1;
                opCode = OpCode.Continuation;
            }

            return frameCount;
        }

        /// <summary>
        /// Write the message to the provided buffer.
        /// 
        /// The return value indicates whether a complete frame was sent. This is
        /// typically not useful, as a message may consist of many frames. Also
        /// many messages may be submitted before a write is called.
        /// </summary>
        /// <param name="buffer">The buffer to write messages to.</param>
        /// <param name="offset">The start of the buffer. This is updated as the message is written.</param>
        /// <returns>True if an entire frame was sent; otherwise false.</returns>
        public bool Process(byte[] buffer, ref long offset)
        {
            return _frameWriter.Process(buffer, ref offset);
        }

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

                    return data.ToFlatArray();
                }
                default:
                    throw new InvalidOperationException("unhandled message type");
            }
        }

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