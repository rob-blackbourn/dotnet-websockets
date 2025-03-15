using System;
using System.Buffers.Binary;

namespace WebSockets.Core
{
    /// <summary>
    /// A class to read WebSocket frames.
    /// 
    /// Data is submitted (<see cref="WriteData"/>) to the reader, then
    /// frames are produced when processed (<see cref="ReadFrame"/>).
    /// </summary>
    internal class FrameReader
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

        private readonly FragmentBuffer<byte> _buffer = new();
        private State _state = State.BYTE1;
        private bool _fin;
        private Reserved _reserved;
        private OpCode _opCode;
        private bool _isMasked;
        private long _payloadLength;
        private byte[] _mask = [];
        private byte[] _payload = [];

        /// <summary>
        /// A property to indicate if the frame reader requires more data to
        /// produce the current frame.
        /// </summary>
        /// <returns>True if there is more data is required; otherwise false.</returns>
        public bool NeedsData => _state != State.BYTE1;

        /// <summary>
        /// Submit data to be processed to frames.
        /// 
        /// After submitting the data <see cref="ReadFrame"/> must be called to
        /// generate the frames.
        /// </summary>
        /// <param name="source">The data to create the frames with.</param>
        /// <param name="offset"><The point to start reading the buffer./param>
        /// <param name="length">The length of the buffer to read.</param>
        public void WriteData(byte[] source, long offset, long length)
        {
            _buffer.Write(source, offset, length);
        }

        /// <summary>
        /// Process submitted data to produce frames.
        /// </summary>
        /// <returns>A frame, if there is sufficient data available.</returns>
        public Frame? ReadFrame()
        {
            if (_state == State.BYTE1)
            {
                if (_buffer.Count < 1)
                    return null;

                var buf = new byte[1];
                _buffer.ReadExactly(buf);

                _fin = (buf[0] & 0b10000000) != 0;
                _reserved = new Reserved(
                    (buf[0] & 0b01000000) != 0,
                    (buf[0] & 0b00100000) != 0,
                    (buf[0] & 0b00010000) != 0);
                _opCode = (OpCode)(buf[0] & 0b00001111);
                _state = State.BYTE2;
            }

            if (_state == State.BYTE2)
            {
                if (_buffer.Count < 1)
                    return null;

                var buf = new byte[1];
                _buffer.ReadExactly(buf);

                _isMasked = (buf[0] & 0b10000000) != 0;

                var length = buf[0] & 0b01111111;
                if (length == 127)
                {
                    _state = State.LONG_LENGTH;
                }
                else if (length == 126)
                {
                    _state = State.SHORT_LENGTH;
                }
                else
                {
                    _payloadLength = length;
                    _state = _isMasked ? State.MASK : State.PAYLOAD;
                }
            }

            if (_state == State.SHORT_LENGTH)
            {
                if (_buffer.Count < 2)
                    return null;
                var buf = new byte[2];
                _buffer.ReadExactly(buf);
                _payloadLength = (ushort)BinaryPrimitives.ReadUInt16BigEndian(buf);
                _state = _isMasked ? State.MASK : State.PAYLOAD;
            }
            else if (_state == State.LONG_LENGTH)
            {
                if (_buffer.Count < 8)
                    return null;
                var buf = new byte[8];
                _buffer.ReadExactly(buf);
                _payloadLength = (ushort)BinaryPrimitives.ReadUInt64BigEndian(buf);
                _state = _isMasked ? State.MASK : State.PAYLOAD;
            }

            if (_state == State.MASK)
            {
                if (_buffer.Count < 4)
                    return null;

                _mask = new byte[4];
                _buffer.ReadExactly(_mask);

                _state = State.PAYLOAD;
            }

            if (_state == State.PAYLOAD)
            {
                if (_buffer.Count < _payloadLength)
                    return null;

                _payload = new byte[_payloadLength];
                _buffer.ReadExactly(_payload);

                if (_isMasked)
                    for (int i = 0; i < _payload.Length; ++i)
                        _payload[i] = (byte)(_payload[i] ^ _mask[i % 4]);

                _state = State.BYTE1;

                return new Frame(_opCode, _fin, _reserved, _isMasked ? _mask : null, _payload);
            }

            throw new InvalidOperationException();
        }
    }
}

