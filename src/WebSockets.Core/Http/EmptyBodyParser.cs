using System;

namespace WebSockets.Core.Http
{
    class EmptyBodyParser : BodyParser
    {
        public override bool NeedsData => false;

        public override bool HasBody => true;

        public override byte[] ReadBody()
        {
            return [];
        }

        public override void WriteData(byte[] array, long offset, long length)
        {
            if (length > 0)
                throw new InvalidOperationException("No data is expected");
        }

        public override void ProcessData()
        {
        }
    }
}
