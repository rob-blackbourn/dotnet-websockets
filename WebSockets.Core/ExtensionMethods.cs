using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace WebSockets.Core
{
    static class ExtensionMethods
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