﻿using System;
using System.Buffers.Binary;

namespace WebSockets.Core
{
    public class FrameReader
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

        private readonly Buffer _buffer = new Buffer();
        private State _state = State.BYTE1;
        private bool _fin, _rsv1, _rsv2, _rsv3;
        private OpCode _opCode;
        private bool _isMasked;
        private int _payloadLength;
        private byte[] _mask = new byte[0];
        private byte[] _payload = new byte[0];

        public void Receive(byte[] data)
        {
            _buffer.EnqueueRange(data);
        }

        public Frame? Process()
        {
            if (_state == State.BYTE1)
            {
                if (_buffer.Count < 1)
                    return null;
                var value = _buffer.Dequeue();
                _fin = (value & 0b10000000) != 0;
                _rsv1 = (value & 0b01000000) != 0;
                _rsv2 = (value & 0b00100000) != 0;
                _rsv3 = (value & 0b00010000) != 0;
                _opCode = (OpCode)(value & 0b00001000);
                _state = State.BYTE1;
            }

            if (_state == State.BYTE2)
            {
                if (_buffer.Count < 1)
                    return null;

                var value = _buffer.Dequeue();

                _isMasked = (value & 0b10000000) != 0;

                var length = value & 0b01111111;
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
                    _payloadLength = (int)length;
                    _state = _isMasked ? State.MASK : State.PAYLOAD;
                }
            }

            if (_state == State.SHORT_LENGTH)
            {
                if (_buffer.Count < 2)
                    return null;
                var buf = _buffer.DequeueRange(2);
                _payloadLength = (ushort)BinaryPrimitives.ReadUInt16BigEndian(buf);
                _state = _isMasked ? State.MASK : State.PAYLOAD;
            }
            else if (_state == State.LONG_LENGTH)
            {
                if (_buffer.Count < 8)
                    return null;
                var buf = _buffer.DequeueRange(8);
                _payloadLength = (ushort)BinaryPrimitives.ReadUInt64BigEndian(buf);
                _state = _isMasked ? State.MASK : State.PAYLOAD;
            }

            if (_state == State.MASK)
            {
                if (_buffer.Count < 4)
                    return null;
                _mask = _buffer.DequeueRange(4);
                _state = State.PAYLOAD;
            }

            if (_state == State.PAYLOAD)
            {
                if (_buffer.Count < _payloadLength)
                    return null;

                _payload = _buffer.DequeueRange(_payloadLength);

                if (_isMasked)
                    for (int i = 0; i < _payload.Length; ++i)
                        _payload[i] = (byte)(_payload[i] ^ _mask[i % 4]);

                _state = State.BYTE1;

                return new Frame(_opCode, _fin, _rsv1, _rsv2, _rsv3, _isMasked ? _mask : null, _payload);
            }

            throw new InvalidOperationException();
        }
    }
}

