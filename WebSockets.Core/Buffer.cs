using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WebSockets.Core
{
    class Buffer
    {
        private readonly LinkedList<ArrayBuffer<byte>> _buffer = new LinkedList<ArrayBuffer<byte>>();

        public long Count { get { return _buffer.Aggregate(0L, (sum, x) => sum + (long)x.Count); }}

        public void WriteByte(byte value)
        {
            _buffer.AddFirst(new ArrayBuffer<byte>([value]));
        }

        public void Write(byte[] values)
        {
            _buffer.AddFirst(values);
        }

        public int ReadByte()
        {
            var buffer = new byte[1];
            var bytesRead = Read(buffer);
            if (bytesRead == 0)
                return -1;

            return buffer[0];
        }

        public long Read(byte[] buffer)
        {
            var offset = 0L;
            while (offset < buffer.LongLength && _buffer.Count > 0)
            {
                var last = _buffer.Last;
                if (last == null)
                    break;

                var bytesRequired = buffer.Length - offset;

                if (last.Value.Array == null)
                {
                    _buffer.RemoveLast();
                    continue;
                }

                if (last.Value.Count <= bytesRequired)
                {
                    Array.Copy(last.Value.Array, buffer, offset);
                    offset += last.Value.Count;
                    if (last.Value.Count == bytesRequired)
                        _buffer.RemoveLast();
                }
                else
                {

                    Array.Copy(last.Value.Array, last.Value.Offset, buffer, offset, bytesRequired);
                    offset += bytesRequired;
                    last.Value = last.Value.Slice(bytesRequired);
                }
            }

            return offset;
        }

        public void ReadExactly(byte[] buffer)
        {
            var bytesRead = Read(buffer);
            if (bytesRead != buffer.LongLength)
                throw new EndOfStreamException();
        }
    }
}