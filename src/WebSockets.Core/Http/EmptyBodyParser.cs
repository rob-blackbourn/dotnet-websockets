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
            throw new NotImplementedException();
        }
    }
}
