using System;
using System.Collections.Generic;

namespace WebSockets.Core
{
    class Buffer
    {
        // TODO: rework to chunked.
        private readonly Queue<byte> _buffer = new Queue<byte>();

        public int Count { get { return _buffer.Count; }}

        public void Enqueue(byte value)
        {
            _buffer.Enqueue(value);
        }

        public void EnqueueRange(byte[] values)
        {
            _buffer.EnsureCapacity(_buffer.Count + values.Length);
            
            foreach (var value in values)
                _buffer.Enqueue(value);
        }

        public byte Dequeue()
        {
            return _buffer.Dequeue();
        }

        public byte[] DequeueRange(int length)
        {
            var buf = new byte[length];
            for (var i = 0; i < length; ++i)
                buf[i] = _buffer.Dequeue();
            return buf;
        }
    }
}