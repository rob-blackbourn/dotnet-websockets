using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace WebSockets.Core
{
    /// <summary>
    /// A class to write WebSocket frames.
    /// 
    /// Frames are submitted (<see cref="WriteFrame"/>) to the writer, and then processed (<see cref="ReadData"/>)
    /// until the writer is empty (<see cref="IsEmpty"/>).
    /// </summary>
    internal class FrameWriter
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

        /// <summary>
        /// A property to indicate if the frame writer has any data to be sent.
        /// </summary>
        /// <returns>True if there is data to be sent; otherwise false.</returns>
        public bool HasData => _frameQueue.Count > 0 || _state != State.BYTE1;

        /// <summary>
        /// Add a frame to the queue of frames to write.
        /// 
        /// Submitted frames can be written with the <see cref="ReadData"/> method. 
        /// </summary>
        /// <param name="frame">The frame to be written</param>
        public void WriteFrame(Frame frame)
        {
            _frameQueue.Enqueue(frame);
        }

        public long ReadData(byte[] destination, long offset, long length)
        {
            if (length > destination.LongLength)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (_frameQueue.Count == 0)
                throw new InvalidOperationException("No frames to read");

            var frame = _frameQueue.Peek();
            var index = offset;

            if (_state == State.BYTE1)
            {
                if (length - index < 1)
                    return index - offset;

                byte value = (byte)(frame.IsFinal ? 0b10000000 : 0);
                value |= (byte)(frame.Reserved.IsRsv1 ? 0b01000000 : 0);
                value |= (byte)(frame.Reserved.IsRsv2 ? 0b00100000 : 0);
                value |= (byte)(frame.Reserved.IsRsv3 ? 0b00010000 : 0);
                value |= (byte)frame.OpCode;

                destination[index] = value;
                index += 1;

                _state = State.BYTE2;
            }

            if (_state == State.BYTE2)
            {
                if (length - index < 1)
                    return index - offset;

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

                destination[index] = value;
                index += 1;
            }

            if (_state == State.SHORT_LENGTH)
            {
                if (_sendBuffer is null)
                {
                    var buf = new byte[2];
                    BinaryPrimitives.WriteUInt16BigEndian(buf, (ushort)frame.Payload.Count);
                    _sendBuffer = new ArrayBuffer<byte>(buf);
                }

                index += _sendBuffer.CopyInto(destination, index, length);

                if (_sendBuffer.Count != 0)
                    return index - offset;

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

                index += _sendBuffer.CopyInto(destination, index, length);

                if (_sendBuffer.Count != 0)
                    return index - offset;

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

                index += _sendBuffer.CopyInto(destination, index, length);

                if (_sendBuffer.Count != 0)
                    return index - offset;

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

                index += _sendBuffer.CopyInto(destination, index, length);

                if (_sendBuffer.Count != 0)
                    return index - offset;

                _sendBuffer = null;
                _frameQueue.Dequeue();

                _state = State.BYTE1;

                return index - offset;
            }

            throw new InvalidOperationException("Invalid state");
        }
    }
}