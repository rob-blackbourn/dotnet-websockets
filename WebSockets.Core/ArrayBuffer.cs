using System;
using System.Linq;

namespace WebSockets.Core
{
    /// <summary>
    /// This class serves a similar purpose to Span and ArraySegment, but
    /// supporting long length and offset.
    /// </summary>
    /// <typeparam name="T">The type of items in the buffer</typeparam>
    class ArrayBuffer<T> : IEquatable<ArrayBuffer<T>>
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

        public long CopyInto(T[] array, long offset, long length)
        {
            var availableLength = long.Min(Array.LongLength - Offset, length - offset);
            if (availableLength > 0)
            {
                System.Array.Copy(Array, Offset, array, offset, availableLength);
                Offset += availableLength;
                Count -= availableLength;
            }
            return availableLength;
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

        public bool Equals(ArrayBuffer<T>? other)
        {
            return other is not null &&
                Offset == other.Offset &&
                Count == other.Count &&
                Array.SequenceEqual(other.Array);
        }

        public override bool Equals(object? obj)
        {
            return base.Equals(obj as ArrayBuffer<T>);
        }

        public override int GetHashCode()
        {
            return Array.GetHashCode() ^ Offset.GetHashCode() ^ Count.GetHashCode();
        }

        public static bool operator ==(ArrayBuffer<T> obj1, ArrayBuffer<T> obj2)
        {
            if (ReferenceEquals(obj1, obj2)) 
                return true;
            if (ReferenceEquals(obj1, null)) 
                return false;
            if (ReferenceEquals(obj2, null))
                return false;
            return obj1.Equals(obj2);
        }
        
        public static bool operator !=(ArrayBuffer<T> obj1, ArrayBuffer<T> obj2) => !(obj1 == obj2);
    }
}