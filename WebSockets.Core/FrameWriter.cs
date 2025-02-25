using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace WebSockets.Core
{
    class FrameWriter
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

        private State _state = State.BYTE1;
        private ArrayBuffer<byte>? _sendBuffer = null;
        private readonly Queue<Frame> _frameQueue = new Queue<Frame>();

        public bool IsEmpty => _frameQueue.Count == 0 && (_sendBuffer is null || _sendBuffer.Count == 0);

        public void Send(Frame frame)
        {
            _frameQueue.Enqueue(frame);
        }

        public bool Write(byte[] buffer, ref long offset)
        {
            if (_frameQueue.Count == 0)
                throw new InvalidOperationException("No frames to write");

            var frame = _frameQueue.Peek();

            if (_state == State.BYTE1)
            {
                if (buffer.Length - offset < 1)
                    return false;

                byte value = (byte)(frame.IsFinal ? 0b10000000 : 0);
                value |= (byte)(frame.Reserved.IsRsv1 ? 0b01000000 : 0);
                value |= (byte)(frame.Reserved.IsRsv2 ? 0b00100000 : 0);
                value |= (byte)(frame.Reserved.IsRsv3 ? 0b00010000 : 0);
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

                if (frame.Payload.Count < 126)
                {
                    value |= (byte)frame.Payload.Count;
                    _state = frame.Mask != null ? State.MASK : State.PAYLOAD;
                }
                else if (frame.Payload.Count <= ushort.MaxValue)
                {
                    value |= (byte)0b01111110;
                    _state = State.SHORT_LENGTH;
                }
                else
                {
                    value |= (byte)0b01111111;
                    _state = State.SHORT_LENGTH;
                }
                
                buffer[offset] = value;
                offset += 1;
            }

            if (_state == State.SHORT_LENGTH)
            {
                if (_sendBuffer is null)
                {
                    var buf = new byte[2];
                    BinaryPrimitives.WriteUInt16BigEndian(buf, (ushort)frame.Payload.Count);
                    _sendBuffer = new ArrayBuffer<byte>(buf);
                }

                offset += _sendBuffer.CopyInto(buffer, offset);

                if (_sendBuffer.Count != 0)
                    return false;

                _sendBuffer = null;

                _state = frame.Mask != null ? State.MASK : State.PAYLOAD;
            }
            else if (_state == State.LONG_LENGTH)
            {
                if (_sendBuffer is null)
                {
                    var buf = new byte[8];
                    BinaryPrimitives.WriteUInt64BigEndian(buf, (ulong)frame.Payload.Count);
                    _sendBuffer = new ArrayBuffer<byte>(buf);
                }

                offset += _sendBuffer.CopyInto(buffer, offset);

                if (_sendBuffer.Count != 0)
                    return false;

                _sendBuffer = null;

                _state = frame.Mask != null ? State.MASK : State.PAYLOAD;
            }

            if (_state == State.MASK)
            {
                if (_sendBuffer is null)
                {
                    if (frame.Mask == null)
                        throw new InvalidOperationException("Mask cannot be null");

                    _sendBuffer = new ArrayBuffer<byte>(frame.Mask);
                }

                offset += _sendBuffer.CopyInto(buffer, offset);

                if (_sendBuffer.Count != 0)
                    return false;

                _sendBuffer = null;

                _state = State.PAYLOAD;
            }

            if (_state == State.PAYLOAD)
            {
                if (_sendBuffer is null)
                {
                    if (frame.Mask == null)
                        _sendBuffer = frame.Payload.Slice(0);
                    else
                    {
                        var buf = new byte[frame.Payload.Count];
                        for (var i = 0L; i < frame.Payload.Count; ++i)
                            buf[i] = (byte)(frame.Payload[i] ^ frame.Mask[i % 4]);
                        _sendBuffer = new ArrayBuffer<byte>(buf);
                    }
                }
                
                offset += _sendBuffer.CopyInto(buffer, offset);

                if (_sendBuffer.Count != 0)
                    return false;

                _sendBuffer = null;
                _frameQueue.Dequeue();

                _state = State.BYTE1;

                return true;
            }

            throw new InvalidOperationException("Invalid state");
        }
    }
}