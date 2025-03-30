using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WebSockets.Core.Http
{
    class FixedLengthBodyParser : BodyParser
    {
        private readonly FragmentBuffer<byte> _buffer;
        private readonly int _contentLength;
        private byte[]? _body = null;

        public FixedLengthBodyParser(int contentLength, FragmentBuffer<byte> buffer)
        {
            _contentLength = contentLength;
            _buffer = buffer;
        }

        public override bool HasBody => _body is not null;
        public override bool NeedsData => _body is null && _buffer.Count < _contentLength;

        public override byte[] ReadBody()
        {
            if (_body is null)
                throw new InvalidOperationException("Body not available");
            return _body;
        }

        public override void WriteData(byte[] array, long offset, long length)
        {
            _buffer.Write(array, offset, length);

            var currentLength = _buffer.Count;
            if (currentLength < _contentLength)
                return;
            if (currentLength > _contentLength)
                throw new InvalidDataException("too much data");

            _body = _buffer.ToArray();
        }
    }
}
