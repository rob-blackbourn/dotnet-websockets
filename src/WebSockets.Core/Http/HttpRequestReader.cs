using System;

namespace WebSockets.Core.Http
{
    class HttpRequestReader : Reader
    {
        private readonly FragmentBuffer<byte> _buffer = new();
        private readonly HeadRequestParser _headParser;
        private BodyReader? _bodyParser = null;
        private HttpRequest? _request = null;
        private RequestHead? _head = null;
        private byte[]? _body = null;

        public HttpRequestReader()
        {
            _headParser = new HeadRequestParser(_buffer);
        }

        public bool NeedsData => _headParser.NeedsData || _bodyParser is null || _bodyParser.NeedsData;
        public bool HasRequest => _request is not null;

        public HttpRequest ReadRequest()
        {
            if (_request == null)
                throw new InvalidOperationException("no request");
            return _request;
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
                    _bodyParser = CreateBodyParser(_head);
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

            if (_request is null && _head is not null && _body is not null)
            {
                _request = new HttpRequest(
                    _head.Verb,
                    _head.Path,
                    _head.Version,
                    _head.Headers,
                    _body
                );
            }
        }

        private BodyReader CreateBodyParser(RequestHead head)
        {
            string? transferEncoding =
                !head.Headers.TryGetValue("transfer-encoding", out var transferEncodings) || transferEncodings.Count == 0
                ? null
                : transferEncodings.Count == 1
                    ? transferEncodings[0].ToLowerInvariant()
                    : throw new InvalidOperationException("Multiple values not allowed for transfer-encoding");

            int? contentLength =
                !head.Headers.TryGetValue("content-length", out var contentLengths) || contentLengths.Count == 0
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
