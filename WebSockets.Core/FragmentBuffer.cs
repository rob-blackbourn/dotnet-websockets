using System;
using System.Collections.Generic;
using System.Globalization;
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

        public T this[long index]
        {
            get
            {
                var node = _buffer.Last;
                if (node is null)
                    throw new ArgumentOutOfRangeException(nameof(index));

                var start = 0L;
                var end = 0L;
                while (node != null)
                {
                    end = start + node.Value.Count;
                    if (index >= start && index < end)
                        return node.Value[index - start];
                    node = node.Previous;
                    start = end;
                }

                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public long IndexOf(T[] pattern, long offset)
        {
            for (long i = offset; i < 1 + Count - pattern.LongLength; ++i)
            {
                bool found = true;
                for (long j = 0L; j < pattern.LongLength; ++j)
                {
                    T a = this[i + j];
                    T b = pattern[j];
                    if (!((a is null && b is null) || (a is not null && b is not null && a.Equals(b))))
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                    return i;
            }
            return -1;
        }

        public bool EndsWith(T[] pattern)
        {
            if (pattern.LongLength > Count)
                return false;
            for (long i = 0L, j = Count - pattern.LongLength; i < pattern.LongLength; ++i, ++j)
            {
                T a = this[j];
                T b = pattern[i];
                if (!((a is null && b is null) || (a is not null && b is not null && a.Equals(b))))
                    return false;
            }
            return true;
        }
    }
}