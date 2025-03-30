using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WebSockets.Core.Http
{
    public class RequestParser
    {
        private readonly FragmentBuffer<byte> _buffer = new();
        private readonly HeadRequestParser _headParser;
        private BodyParser? _bodyParser = null;
        private Request? _request = null;
        private RequestHead? _head = null;
        private byte[]? _body = null;

        public RequestParser()
        {
            _headParser = new HeadRequestParser(_buffer);
        }

        public bool NeedsData => _headParser.NeedsData || _bodyParser is null || _bodyParser.NeedsData;
        public bool HasRequest => _request is not null;

        public Request ReadRequest()
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
                _request = new Request(
                    _head.Verb,
                    _head.Path,
                    _head.Version,
                    _head.Headers,
                    _body
                );
            }
        }

        private BodyParser CreateBodyParser(RequestHead head)
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
                return new ChunkedBodyParser(_buffer);
            }

            if (transferEncoding != null)
                throw new InvalidOperationException("Only chunked is supported for transfer-encoding");

            return contentLength.HasValue
                ? new FixedLengthBodyParser(contentLength.Value, _buffer)
                : new EmptyBodyParser();
        }
    }

    /// <summary>
    /// A class modelling the required values of a WebSocket HTTP request.
    /// </summary>
    public class RequestReader
    {
        private enum State
        {
            Instruction,
            Header,
            BodyChunked,
            BodyLength,
            UnusedBytes,
            Done
        }

        private abstract class BodyParser
        {
            private readonly Instruction _instruction;
            private readonly IDictionary<string, IList<string>> _headers;

            protected BodyParser(
                Instruction instruction,
                IDictionary<string, IList<string>> headers)
            {
                _instruction = instruction;
                _headers = headers;
            }

            public Request ToRequest(byte[]? body)
            {
                return new Request(
                        _instruction.Verb,
                        _instruction.Path,
                        _instruction.Version,
                        _headers,
                        body
                    );
            }
        }

        private class BodyLengthParser : BodyParser
        {
            public BodyLengthParser(
                Instruction instruction,
                IDictionary<string, IList<string>> headers,
                int contentLength)
                : base(instruction, headers)
            {
                ContentLength = contentLength;
            }
            public int ContentLength { get; }
        }

        private class BodyChunkedParser : BodyParser
        {
            public BodyChunkedParser(
                Instruction instruction,
                IDictionary<string, IList<string>> headers)
                : base(instruction, headers)
            {
            }

            public int? ChunkLength { get; set; } = null;
            public FragmentBuffer<byte> Chunks { get; } = new();
        }

        private class Instruction
        {
            public Instruction(string verb, string path, string version)
            {
                Verb = verb;
                Path = path;
                Version = version;
            }

            public string Verb { get; }
            public string Path { get; }
            public string Version { get; }
        }

        internal static byte[] EOL = "\r\n"u8.ToArray();
        internal static byte[] EOM = "\r\n\r\n"u8.ToArray();

        private readonly FragmentBuffer<byte> _buffer = new();
        private State _state = State.Instruction;
        private Instruction? _instruction = null;
        private readonly IDictionary<string, IList<string>> _headers = new Dictionary<string, IList<string>>(StringComparer.InvariantCultureIgnoreCase);
        private BodyLengthParser? _bodyLengthParser = null;
        private BodyChunkedParser? _bodyChuckedParser = null;

        public Request? Request { get; private set; } = null;
        public byte[] UnusedBytes { get; private set; } = [];

        public void WriteData(byte[] array, long offset, long length)
        {
            _buffer.Write(array, offset, length);

            if (_state == State.Instruction)
            {
                var index = _buffer.IndexOf(EOL);
                if (index == -1)
                    return;

                var line = new byte[index + EOL.Length];
                _buffer.ReadExactly(line);
                var text = Encoding.UTF8.GetString(line, 0, (int)index);

                var parts = text.Split(' ');
                if (parts.Length != 3)
                    throw new InvalidDataException("The request line should have three parts separated by spaces");

                _instruction = new Instruction(parts[0], parts[1], parts[1]);
                _state = State.Header;
            }

            if (_state == State.Header)
            {
                while (true)
                {
                    var index = _buffer.IndexOf(EOL);
                    if (index == -1)
                        break;

                    var line = new byte[index + EOL.Length];
                    _buffer.ReadExactly(line);
                    var text = Encoding.UTF8.GetString(line, 0, (int)index);
                    if (text == "\r\n")
                    {
                        string? transferEncoding =
                            !_headers.TryGetValue("transfer-encoding", out var transferEncodings) || transferEncodings.Count == 0
                            ? null
                            : transferEncodings.Count == 1
                                ? transferEncodings[0].ToLowerInvariant()
                                : throw new InvalidOperationException("Multiple values not allowed for transfer-encoding");

                        int? contentLength =
                            !_headers.TryGetValue("content-length", out var contentLengths) || contentLengths.Count == 0
                            ? null
                            : contentLengths.Count == 1
                                ? int.Parse(contentLengths[0])
                                : throw new InvalidOperationException("Multiple values not allowed for content-length");

                        if (transferEncoding == "chunked")
                        {
                            if (contentLength.HasValue)
                                throw new InvalidOperationException("Cannot specify content-length if transfer-encoding is chunked");
                            _state = State.BodyChunked;
                        }
                        else if (transferEncoding != null)
                        {
                            throw new InvalidOperationException("Only chunked is supported for transfer-encoding");

                        }
                        else
                        {
                            if (_instruction is null)
                                throw new InvalidOperationException("invalid body state");

                            if (contentLength.HasValue)
                            {
                                _bodyLengthParser = new BodyLengthParser(_instruction, _headers, contentLength.Value);
                                _state = State.BodyLength;
                            }
                            else
                            {
                                _bodyChuckedParser = new BodyChunkedParser(_instruction, _headers);
                                _state = State.BodyChunked;
                            }
                        }

                        break;
                    }

                    var colon = text.IndexOf(':');
                    if (colon == -1)
                        throw new InvalidDataException("A header line should contain a colon");
                    var name = text.Substring(0, colon).Trim().ToLowerInvariant();
                    var value = text.Substring(colon + 1).Trim();
                    if (!_headers.TryGetValue(name, out var values))
                        _headers.Add(name, values = []);
                    values.Add(value);
                }
            }

            if (_state == State.BodyChunked)
            {
                if (_bodyChuckedParser is null)
                    throw new InvalidOperationException("Invalid body chunked state");

                if (!_bodyChuckedParser.ChunkLength.HasValue)
                {
                    var i = _buffer.IndexOf(EOL);

                    if (i == -1)
                        return; // Not enough data
                    else if (i == 0)
                    {
                        // No chunks.
                        if (_bodyChuckedParser.Chunks.Count > 0)
                            throw new InvalidOperationException("bad chunk termination");

                        // Consume buffer.
                        var buf = new byte[EOL.Length];
                        _buffer.ReadExactly(buf);
                        Request = _bodyChuckedParser.ToRequest(null);
                    }
                    else
                    {
                        var buf = new byte[i + EOL.Length];
                        _buffer.ReadExactly(buf);
                        var text = Encoding.ASCII.GetString(buf, 0, (int)i);
                        _bodyChuckedParser.ChunkLength = Convert.ToInt32(text, 16);
                    }
                }

                if (_bodyChuckedParser.ChunkLength.HasValue)
                {
                    if (_bodyChuckedParser.ChunkLength.Value == 0)
                    {
                        if (_buffer.Count < EOL.Length)
                            return;
                        var buf = new byte[EOL.Length];
                        _buffer.ReadExactly(buf);
                        if (!buf.SequenceEqual(EOL))
                            throw new InvalidOperationException("Invalid termination of chunk stream");
                        var body = _bodyChuckedParser.Chunks.ToArray();

                    }
                }
            }
            else if (_state == State.BodyLength)
            {
                if (_bodyLengthParser is null)
                    throw new InvalidOperationException("Invalid body length state");

                var currentLength = _buffer.Count;
                if (currentLength >= _bodyLengthParser.ContentLength)
                {
                    var body = new byte[_bodyLengthParser.ContentLength];
                    _buffer.ReadExactly(body);
                    Request = _bodyLengthParser.ToRequest(body);

                    if (_buffer.Count > 0)
                    {
                        UnusedBytes = new byte[_buffer.Count];
                        _buffer.ReadExactly(UnusedBytes);
                    }
                    _state = State.Done;
                    return;
                }
            }
        }
    }
}
