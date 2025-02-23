using System;

namespace WebSockets.Core
{
    public struct Reserved : IEquatable<Reserved>
    {
        public static Reserved AllFalse {get;} = new Reserved(false, false, false);

        public Reserved(bool isRsv1, bool isRsv2, bool isRsv3)
        {
            IsRsv1 = isRsv1;
            IsRsv2 = isRsv2;
            IsRsv3 = isRsv3;
        }

        public bool IsRsv1 { get; private set; }
        public bool IsRsv2 { get; private set; }
        public bool IsRsv3 { get; private set; }

        public bool Equals(Reserved other)
        {
            return
                IsRsv1 == other.IsRsv1 &&
                IsRsv2 == other.IsRsv2 &&
                IsRsv3 == other.IsRsv3;
        }
    }
}

