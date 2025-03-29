using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WebSockets.Core.Http
{
    class FixedLengthBodyParser : BodyParser
    {
        private readonly FragmentBuffer<byte> _buffer = new();
        private readonly int _contentLength;
        private Request? _request;
        private byte[]? _unusedBytes;

        public FixedLengthBodyParser(
            string verb,
            string path,
            string version,
            IDictionary<string, IList<string>> headers,
            int contentLength)
            : base(verb, path, version, headers)
        {
            _contentLength = contentLength;
        }

        public bool HasRequest => _request is not null;
        public bool NeedsData => _request is null && _buffer.Count < _contentLength;

        public Request ReadRequest()
        {
            if (_request is null)
                throw new InvalidOperationException("Request not available");
            return _request;
        }

        public void WriteData(byte[] array, long offset, long length)
        {
            _buffer.Write(array, offset, length);

            var currentLength = _buffer.Count;
            if (currentLength < _contentLength)
                return;
            if (currentLength > _contentLength)
                throw new InvalidDataException("too much data");

            _request = ToRequest(_buffer.ToArray());
        }
    }
}
