using System;
using System.Collections.Generic;

namespace WebSockets.Core.Http
{
    class HttpResponseReader : Reader
    {
        private readonly FragmentBuffer<byte> _buffer = new();
        private readonly HeadResponseParser _headParser;
        private BodyReader? _bodyParser = null;
        private HttpResponse? _response = null;
        private ResponseHead? _head = null;
        private byte[]? _body = null;

        public HttpResponseReader()
        {
            _headParser = new HeadResponseParser(_buffer);
        }

        public bool NeedsData => _headParser.NeedsData || _bodyParser is null || _bodyParser.NeedsData;
        public bool HasResponse => _response is not null;

        public HttpResponse ReadResponse()
        {
            if (_response == null)
                throw new InvalidOperationException("no response");
            return _response;
        }

        public void WriteData(byte[] data, long offset, long length)
        {
            _buffer.Write(data, offset, length);

            if (_head is null)
            {
                if (!_headParser.HasHead)
                {
                    _headParser.ProcessData();
                }

                if (_headParser.HasHead)
                {
                    _head = _headParser.ReadHead();
                }
            }

            if (_head is not null && _body is null)
            {
                if (_bodyParser is null)
                {
                    _bodyParser = CreateBodyParser(_head.Headers);
                }

                if (!_bodyParser.HasBody)
                {
                    _bodyParser.ProcessData();
                }

                if (_bodyParser.HasBody)
                {
                    _body = _bodyParser.ReadBody();
                }
            }

            if (_response is null && _head is not null && _body is not null)
            {
                _response = new HttpResponse(
                    _head.Version,
                    _head.Code,
                    _head.Reason,
                    _head.Headers,
                    _body
                );
            }
        }

        private BodyReader CreateBodyParser(IDictionary<string, IList<string>> headers)
        {
            string? transferEncoding =
                !headers.TryGetValue("transfer-encoding", out var transferEncodings) || transferEncodings.Count == 0
                ? null
                : transferEncodings.Count == 1
                    ? transferEncodings[0].ToLowerInvariant()
                    : throw new InvalidOperationException("Multiple values not allowed for transfer-encoding");

            int? contentLength =
                !headers.TryGetValue("content-length", out var contentLengths) || contentLengths.Count == 0
                ? null
                : contentLengths.Count == 1
                    ? int.Parse(contentLengths[0])
                    : throw new InvalidOperationException("Multiple values not allowed for content-length");


            if (transferEncoding == "chunked")
            {
                if (contentLength.HasValue)
                    throw new InvalidOperationException("Cannot specify content-length if transfer-encoding is chunked");
                return new ChunkedBodyReader(_buffer);
            }

            if (transferEncoding != null)
                throw new InvalidOperationException("Only chunked is supported for transfer-encoding");

            return contentLength.HasValue
                ? new HttpFixedLengthBodyReader(contentLength.Value, _buffer)
                : new EmptyBodyReader();
        }
    }
}
