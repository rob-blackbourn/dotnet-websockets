using System;
using System.Linq;

namespace WebSockets.Core
{
    /// <summary>
    /// This class serves a similar purpose to Span and ArraySegment, but
    /// supporting long length and offset.
    /// </summary>
    /// <typeparam name="T">The type of items in the buffer</typeparam>
    internal class ArrayBuffer<T> : IEquatable<ArrayBuffer<T>>
    {
        /// <summary>
        /// Construct an array buffer.
        /// </summary>
        /// <param name="buffer">The buffer to use.</param>
        public ArrayBuffer(T[] buffer)
            : this(buffer, 0, buffer.LongLength)
        {
        }

        /// <summary>
        /// Construct an array buffer.
        /// </summary>
        /// <param name="buffer">The buffer to use.</param>
        /// <param name="offset">The index of the start of the buffer.</param>
        /// <param name="count">The number of items in the buffer.</param>
        public ArrayBuffer(T[] buffer, long offset, long count)
        {
            Buffer = buffer;
            Offset = offset;
            Count = count;
        }

        /// <summary>
        /// The array buffer data.
        /// </summary>
        /// <value>The buffer.</value>
        public T[] Buffer { get; private set; }
        /// <summary>
        /// The start of the array buffer data.
        /// </summary>
        /// <value>The start of the data.</value>
        public long Offset { get; private set; }
        /// <summary>
        /// The number of available values.
        /// </summary>
        /// <value>The length of available data.</value>
        public long Count { get; private set; }

        /// <summary>
        /// Take a copy of the array starting at the given point.
        /// </summary>
        /// <param name="start">The start of the array to copy.</param>
        /// <returns>A copy of the array from the given point.</returns>
        public ArrayBuffer<T> Slice(long start)
        {
            if (Offset + start > Buffer.LongLength)
                throw new ArgumentOutOfRangeException(nameof(start));
            return new ArrayBuffer<T>(Buffer, Offset + start, Count - start);
        }

        /// <summary>
        /// Take a copy of the array from a given start point and for a given number of values.
        /// </summary>
        /// <param name="start">The start of the array to copy.</param>
        /// <param name="count">The number of values to copy.</param>
        /// <returns>A copy of the array from the given start point and number of values.</returns>
        public ArrayBuffer<T> Slice(long start, long count)
        {
            if (Offset + start > Buffer.LongLength)
                throw new ArgumentOutOfRangeException(nameof(start));
            if (Offset + start + count > Buffer.LongLength)
                throw new ArgumentOutOfRangeException(nameof(count));
            return new ArrayBuffer<T>(Buffer, Offset + start, count);
        }

        /// <summary>
        /// Copy up to the given number of bytes into a buffer at a given offset,
        /// advancing the internal offset by the number of bytes copied.
        /// </summary>
        /// <param name="buffer">The destination buffer.</param>
        /// <param name="offset">The offset into the destination buffer.</param>
        /// <param name="length">The number of bytes to copy.</param>
        /// <returns>The number of bytes remaining.</returns>
        public long CopyInto(T[] buffer, long offset, long length)
        {
            var availableLength = long.Min(Buffer.LongLength - Offset, length - offset);
            if (availableLength > 0)
            {
                Array.Copy(Buffer, Offset, buffer, offset, availableLength);
                Offset += availableLength;
                Count -= availableLength;
            }
            return availableLength;
        }

        /// <summary>
        /// Create an buffer from an array.
        /// </summary>
        /// <param name="buffer">The buffer to wrap.</param>
        /// <typeparam name="T">The type of the values in the buffer.</typeparam>
        public static implicit operator ArrayBuffer<T>(T[] buffer) => new ArrayBuffer<T>(buffer);

        /// <summary>
        /// Access the buffer. The index is relative to the internal offset.
        /// </summary>
        /// <value>The accessed value.</value>
        public T this[long index]
        {
            get
            {
                return Buffer[Offset + index];
            }
            set
            {
                Buffer[Offset + index] = value;
            }
        }

        /// <summary>
        /// Create an array of the contents of the internal array from the internal offset.
        /// </summary>
        /// <returns>A copy of the buffer from the offset.</returns>
        public T[] ToArray()
        {
            if (Offset == 0 && Count == Buffer.LongLength)
                return Buffer;

            var buf = new T[Count];
            Array.Copy(Buffer, Offset, buf, 0, Count);
            return buf;
        }

        /// <inheritdoc />
        public bool Equals(ArrayBuffer<T>? other)
        {
            return other is not null &&
                Offset == other.Offset &&
                Count == other.Count &&
                Buffer.SequenceEqual(other.Buffer);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return base.Equals(obj as ArrayBuffer<T>);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Buffer.GetHashCode() ^ Offset.GetHashCode() ^ Count.GetHashCode();
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public static bool operator !=(ArrayBuffer<T> obj1, ArrayBuffer<T> obj2) => !(obj1 == obj2);
    }
}