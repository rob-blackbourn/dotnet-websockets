using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WebSockets.Core
{
    /// <summary>
    /// A double ended queue where fragments of data are fed in, and
    /// consolidated data is read out. This reduces the amount of buffer
    /// reallocation.
    /// </summary>
    /// <typeparam name="T">The type of items in the buffer</typeparam>
    class FragmentBuffer<T>
    {
        private readonly LinkedList<ArrayBuffer<T>> _buffer = new LinkedList<ArrayBuffer<T>>();

        public long Count { get { return _buffer.Aggregate(0L, (sum, x) => sum + (long)x.Count); }}

        public void Write(T[] values)
        {
            _buffer.AddFirst(values);
        }

        public long Read(T[] buffer)
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
                    Array.Copy(last.Value.Array, last.Value.Offset, buffer, offset, last.Value.Count);
                    offset += last.Value.Count;
                    if (last.Value.Count <= bytesRequired)
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

        public void ReadExactly(T[] buffer)
        {
            var TsRead = Read(buffer);
            if (TsRead != buffer.LongLength)
                throw new EndOfStreamException();
        }
    }
}