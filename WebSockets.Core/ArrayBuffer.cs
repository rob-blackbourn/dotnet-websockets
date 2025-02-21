using System;

namespace WebSockets.Core
{
    public class ArrayBuffer<T>
    {
        public ArrayBuffer(T[] array)
            : this(array, 0, array.LongLength)
        {
        }

        public ArrayBuffer(T[] array, long offset, long count)
        {
            Array = array;
            Offset = offset;
            Count = count;
        }

        public T[] Array { get; private set; }
        public long Offset { get; private set; }
        public long Count { get; private set; }

        public ArrayBuffer<T> Slice(long index)
        {
            if (Offset + index > Array.LongLength)
                throw new ArgumentOutOfRangeException(nameof(index));
            return new ArrayBuffer<T>(Array, Offset + index, Count - index);
        }

        public ArrayBuffer<T> Slice(long index, long count)
        {
            if (Offset + index > Array.LongLength)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (Offset + index + count > Array.LongLength)
                throw new ArgumentOutOfRangeException(nameof(count));
            return new ArrayBuffer<T>(Array, Offset + index, count);
        }

        public long CopyInto(byte[] array, long offset)
        {
            var length = long.Min(Array.LongLength - Offset, array.LongLength - offset);
            if (length > 0)
            {
                System.Array.Copy(Array, Offset, array, offset, length);
                Offset += length;
            }
            return length;
        }

        public static implicit operator ArrayBuffer<T>(T[] array) => new ArrayBuffer<T>(array);

        public T this[long index]
        {
            get
            {
                return Array[Offset + index];
            }
            set
            {
                Array[Offset + index] = value;
            }
        }

        public T[] ToArray()
        {
            if (Offset == 0 && Count == Array.LongLength)
                return Array;
                
            var buf = new T[Count];
            System.Array.Copy(Array, Offset, buf, 0, Count);
            return buf;
        }
    }
}