using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSockets.Core
{
    public class MessageWriter
    {
        private readonly INonceGenerator _nonceGenerator;
        private readonly FrameWriter _frameWriter = new FrameWriter();

        public MessageWriter(INonceGenerator nonceGenerator)
        {
            _nonceGenerator = nonceGenerator;
        }

        public void Write(Message message, bool isClient, Reserved reserved, long maxFrameSize = long.MaxValue)
        {
            var opCode = GetOpCode(message.Type);
            var payload = GetPayload(message);
            
            while (payload.Count > 0 || opCode != OpCode.Continuation)
            {
                var length = long.Min(payload.Count, maxFrameSize);
                var framePayload = payload.Slice(0, length);
                payload = payload.Slice(length);
                var isFinal = payload.Count == 0;
                var mask = isClient ? _nonceGenerator.Create() : null;
                var frame = new Frame(opCode, isFinal, reserved, mask, framePayload);
                _frameWriter.Frames.Enqueue(frame);
                opCode = OpCode.Continuation;
            }
        }

        public bool Write(byte[] buffer, ref long offset)
        {
            return _frameWriter.Write(buffer, ref offset);
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