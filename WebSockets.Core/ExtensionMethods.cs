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
    }
}