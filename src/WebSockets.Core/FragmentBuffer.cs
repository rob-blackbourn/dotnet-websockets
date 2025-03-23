using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WebSockets.Core
{
    /// <summary>
    /// A double ended queue where fragments of data are written, and
    /// consolidated data is read out. This reduces the amount of buffer
    /// reallocation.
    /// </summary>
    /// <typeparam name="T">The type of items in the buffer</typeparam>
    internal class FragmentBuffer<T>
    {
        private readonly LinkedList<ArrayBuffer<T>> _buffer = new();

        public long Count => _buffer.Aggregate(0L, (sum, x) => sum + x.Count);
        public bool IsEmpty => _buffer.Count == 0;

        public void Write(T[] array)
        {
            Write(array, 0, array.LongLength);
        }

        public void Write(T[] array, long offset, long length)
        {
            // We can simplify knowing if the buffer is empty by ensuring that
            // fragments must have bytes.
            if (length <= 0)
                return;

            _buffer.AddFirst(new ArrayBuffer<T>(array, offset, length));
        }

        public long Read(T[] destination)
        {
            return Read(destination, 0, destination.LongLength);
        }

        public long Read(T[] destination, long offset, long length)
        {
            while (offset < length && _buffer.Count > 0)
            {
                var last = _buffer.Last;
                if (last == null)
                    break;

                var bytesRequired = length - offset;

                if (last.Value.Buffer == null)
                {
                    _buffer.RemoveLast();
                    continue;
                }

                if (last.Value.Count <= bytesRequired)
                {
                    Array.Copy(
                        last.Value.Buffer,
                        last.Value.Offset,
                        destination,
                        offset,
                        last.Value.Count);
                    offset += last.Value.Count;
                    if (last.Value.Count <= bytesRequired)
                        _buffer.RemoveLast();
                }
                else
                {

                    Array.Copy(
                        last.Value.Buffer,
                        last.Value.Offset,
                        destination,
                        offset,
                        bytesRequired);
                    offset += bytesRequired;
                    last.Value = last.Value.Slice(bytesRequired);
                }
            }

            return offset;
        }

        public void ReadExactly(T[] buffer)
        {
            var bytesRead = Read(buffer, 0L, buffer.LongLength);
            if (bytesRead != buffer.LongLength)
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
            // TODO: Convert to reverse search for speed-up.
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

        public T[] ToArray()
        {
            var buf = new T[Count];
            var offset = 0L;
            foreach (var item in _buffer.Reverse())
            {
                Array.Copy(item.Buffer, item.Offset, buf, offset, item.Count);
                offset += item.Count;
            }

            _buffer.Clear();

            return buf;
        }
    }
}