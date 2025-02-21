using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace WebSockets.Core
{
    public class FrameWriter
    {
        enum State
        {
            BYTE1,
            BYTE2,
            SHORT_LENGTH,
            LONG_LENGTH,
            MASK,
            PAYLOAD
        }

        class SendBuffer
        {
            public SendBuffer(byte[] buffer)
            {
                Buffer = buffer;
                Offset = 0;
            }

            public byte[] Buffer { get; private set; }
            public int Offset { get; private set; }
            public int BytesUnwritten => Buffer.Length - Offset;

            public int CopyTo(byte[] buffer, int offset)
            {
                var length = int.Min(Buffer.Length - Offset, buffer.Length - offset);
                if (length > 0)
                {
                    Array.Copy(Buffer, Offset, buffer, offset, length);
                    Offset += length;
                }
                return length;
            }
        }

        private readonly Queue<Frame> _frameQueue = new Queue<Frame>();
        private State _state = State.BYTE1;
        private SendBuffer? _sendBuffer = null;
        
        public void WriteFrame(Frame frame)
        {
            _frameQueue.Enqueue(frame);
        }

        public bool Write(byte[] buffer, ref int offset)
        {
            if (_frameQueue.Count == 0)
                throw new InvalidOperationException("No frames to write");

            var frame = _frameQueue.Peek();

            if (_state == State.BYTE1)
            {
                if (buffer.Length - offset < 1)
                    return false;

                byte value = (byte)(frame.IsFinal ? 0b10000000 : 0);
                value |= (byte)(frame.IsRsv1 ? 0b01000000 : 0);
                value |= (byte)(frame.IsRsv2 ? 0b00100000 : 0);
                value |= (byte)(frame.IsRsv3 ? 0b00010000 : 0);
                value |= (byte)frame.OpCode;

                buffer[offset] = value;
                offset += 1;

                _state = State.BYTE2;
            }

            if (_state == State.BYTE2)
            {
                if (buffer.Length - offset < 1)
                    return false;

                byte value = (byte)(frame.Mask == null ? 0 : 0b10000000);

                if (frame.Payload.Length < 126)
                {
                    value |= (byte)frame.Payload.Length;
                    _state = frame.Mask != null ? State.MASK : State.PAYLOAD;
                }
                else if (frame.Payload.Length <= ushort.MaxValue)
                {
                    value |= (byte)0b01111110;
                    _state = State.SHORT_LENGTH;
                }
                else
                {
                    value |= (byte)0b01111111;
                    _state = State.SHORT_LENGTH;
                }
                
                buffer[0] = value;
                offset += 1;
            }

            if (_state == State.SHORT_LENGTH)
            {
                if (_sendBuffer == null)
                {
                    var buf = new byte[2];
                    BinaryPrimitives.WriteUInt16BigEndian(buf, (ushort)frame.Payload.Length);
                    _sendBuffer = new SendBuffer(buf);
                }

                offset += _sendBuffer.CopyTo(buffer, offset);

                if (_sendBuffer.BytesUnwritten != 0)
                    return false;

                _sendBuffer = null;

                _state = frame.Mask != null ? State.MASK : State.PAYLOAD;
            }
            else if (_state == State.LONG_LENGTH)
            {
                if (_sendBuffer == null)
                {
                    var buf = new byte[8];
                    BinaryPrimitives.WriteUInt64BigEndian(buf, (ulong)frame.Payload.Length);
                    _sendBuffer = new SendBuffer(buf);
                }

                offset += _sendBuffer.CopyTo(buffer, offset);

                if (_sendBuffer.BytesUnwritten != 0)
                    return false;

                _sendBuffer = null;

                _state = frame.Mask != null ? State.MASK : State.PAYLOAD;
            }

            if (_state == State.MASK)
            {
                if (_sendBuffer == null)
                {
                    if (frame.Mask == null)
                        throw new InvalidOperationException("Mask cannot be null");

                    _sendBuffer = new SendBuffer(frame.Mask);
                }

                offset += _sendBuffer.CopyTo(buffer, offset);

                if (_sendBuffer.BytesUnwritten != 0)
                    return false;

                _sendBuffer = null;

                _state = State.PAYLOAD;
            }

            if (_state == State.PAYLOAD)
            {
                if (_sendBuffer == null)
                {
                    _sendBuffer = new SendBuffer(frame.Payload);
                }
                
                offset += _sendBuffer.CopyTo(buffer, offset);

                if (_sendBuffer.BytesUnwritten != 0)
                    return true;

                _sendBuffer = null;

                _state = State.BYTE1;
            }

            throw new InvalidOperationException("Invalid state");
        }
    }
}