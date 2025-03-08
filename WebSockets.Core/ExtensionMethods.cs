using System;
using System.Collections.Generic;
using System.Linq;

namespace WebSockets.Core
{
    public static class PublicExtensionMethods
    {
        public static string? SingleValue(this IDictionary<string, IList<string>> headers, string key)
        {
            if (!headers.TryGetValue(key, out var list) || list.Count == 0)
                return null;
            if (list.Count > 1)
                throw new InvalidOperationException("header contains multiple values");
            return list[0];
        }

        public static string[]? SingleCommaValues(this IDictionary<string, IList<string>> headers, string key)
        {
            return headers.SingleValue(key)?.Split(',', StringSplitOptions.TrimEntries);
        }
    }

    static class InternalExtensionMethods
    {
        public static T[] ToFlatArray<T>(this IList<T[]> buffers)
        {
            var length = buffers.Sum(x => x.LongLength);
            var buf = new T[length];
            var offset = 0L;
            foreach (var item in buffers)
            {
                Array.Copy(item, 0, buf, offset, item.Length);
                offset += item.Length;
            }

            return buf;
        }

        public static T[] ToFlatArray<T>(this IList<ArrayBuffer<T>> buffers)
        {
            var length = buffers.Sum(x => x.Count);
            var buf = new T[length];
            var offset = 0L;
            foreach (var item in buffers)
            {
                Array.Copy(item.Array, item.Offset, buf, offset, item.Count);
                offset += item.Count;
            }

            return buf;
        }

        public static int IndexOf<T>(this T[] data, T[] pattern)
        where T : struct
        {
            for (var i = 0; i < 1 + data.Length - pattern.Length; ++i)
            {
                bool found = true;
                for (var j = 0; j < pattern.Length; ++j)
                {
                    T a = data[i + j];
                    T b = pattern[j];
                    if (!a.Equals(b))
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

        public static T[] SubArray<T>(this T[] data, int start)
        where T : struct
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException("start must be zero or positive");
            if (start > data.Length)
                throw new ArgumentOutOfRangeException("start is past the end of the array");
            if (start == data.Length)
                return new T[0];
            var length = data.Length - start;
            var copy = new T[length];
            Array.Copy(data, start, copy, 0, length);
            return data;
        }

        public static T[] SubArray<T>(this T[] data, int start, int count)
        where T : struct
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException("start must be zero or positive");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count must be zero or positive");
            if (start + count > data.Length)
                throw new ArgumentOutOfRangeException("insufficient data");
            if (count == 0 || start == data.Length)
                return new T[0];
            var copy = new T[count];
            Array.Copy(data, start, copy, 0, count);
            return data;
        }

    }
}