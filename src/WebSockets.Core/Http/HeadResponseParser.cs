using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WebSockets.Core.Http
{
    /// <summary>
    /// A class modelling the required values of a WebSocket HTTP response.
    /// </summary>
    class HeadResponseParser : Parser
    {
        private class StatusLine
        {
            public StatusLine(string version, int code, string reason)
            {
                Version = version;
                Code = code;
                Reason = reason;
            }

            public string Version { get; }
            public int Code { get; }
            public string Reason { get; }
        }

        private readonly FragmentBuffer<byte> _buffer;
        private StatusLine? _statusLine = null;
        private readonly IDictionary<string, IList<string>> _headers = new Dictionary<string, IList<string>>(StringComparer.InvariantCultureIgnoreCase);

        public HeadResponseParser(FragmentBuffer<byte> buffer)
        {
            _buffer = buffer;
        }

        private ResponseHead? _head = null;

        public bool NeedsData => _head is null;
        public bool HasHead => _head is not null;

        public ResponseHead ReadHead()
        {
            if (_head is null)
                throw new InvalidOperationException("head not ready");
            return _head;
        }

        public void WriteData(byte[] array, long offset, long length)
        {
            _buffer.Write(array, offset, length);

            ProcessData();
        }

        public void ProcessData()
        {
            if (_statusLine is null)
            {
                ProcessStatusLine();
            }

            if (_statusLine is not null)
            {
                ProcessHeaders();
            }
        }

        private void ProcessStatusLine()
        {
            var index = _buffer.IndexOf(EOL);
            if (index == -1)
                return;

            var line = new byte[index + EOL.Length];
            _buffer.ReadExactly(line);
            var text = Encoding.UTF8.GetString(line, 0, (int)index);

            var parts = text.Split(' ', 3);
            if (parts.Length != 3)
                throw new InvalidDataException("The response line should have three parts separated by spaces");
            if (!int.TryParse(parts[1], out var code))
                throw new InvalidDataException("The response code should be an integer");

            _statusLine = new StatusLine(parts[0], code, parts[2]);
        }

        private void ProcessHeaders()
        {
            while (true)
            {
                var index = _buffer.IndexOf(EOL);
                if (index == -1)
                    break;

                var line = new byte[index + EOL.Length];
                _buffer.ReadExactly(line);

                if (index == 0)
                {
                    if (!line.SequenceEqual(EOL))
                        throw new InvalidOperationException("Expected cr/lf");

                    if (_statusLine is null)
                        throw new InvalidOperationException("Head termination received before instruction line");

                    _head = new ResponseHead(
                        _statusLine.Version,
                        _statusLine.Code,
                        _statusLine.Reason,
                        _headers
                    );

                    break;
                }

                var text = Encoding.UTF8.GetString(line, 0, (int)index);
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
    }
}
