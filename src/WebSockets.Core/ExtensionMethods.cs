using System;
using System.Collections.Generic;
using System.Linq;

namespace WebSockets.Core
{
    internal static class PublicExtensionMethods
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
            ArgumentOutOfRangeException.ThrowIfNegative(start);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(start, data.Length);
            if (start == data.Length)
                return [];
            var length = data.Length - start;
            var copy = new T[length];
            Array.Copy(data, start, copy, 0, length);
            return copy;
        }

        public static T[] SubArray<T>(this T[] data, int start, int count)
        where T : struct
        {
            ArgumentOutOfRangeException.ThrowIfNegative(start);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (start + count > data.Length)
                throw new ArgumentOutOfRangeException(nameof(data), "insufficient data");
            if (count == 0 || start == data.Length)
                return [];
            var copy = new T[count];
            Array.Copy(data, start, copy, 0, count);
            return copy;
        }
    }
}