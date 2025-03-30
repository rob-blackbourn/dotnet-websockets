using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Text;

namespace WebSockets.Core.Http
{
    class RequestHead
    {
        public RequestHead(
            string verb,
            string path,
            string version,
            IDictionary<string, IList<string>> headers)
        {
            Verb = verb;
            Path = path;
            Version = version;
            Headers = headers;
        }

        public string Verb { get; }
        public string Path { get; }
        public string Version { get; }
        public IDictionary<string, IList<string>> Headers { get; set; }
    }

    /// <summary>
    /// A class modelling the required values of a WebSocket HTTP request.
    /// </summary>
    class HeadRequestParser
    {
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

        private readonly FragmentBuffer<byte> _buffer;
        private Instruction? _instruction = null;
        private readonly IDictionary<string, IList<string>> _headers = new Dictionary<string, IList<string>>(StringComparer.InvariantCultureIgnoreCase);

        public HeadRequestParser(FragmentBuffer<byte> buffer)
        {
            _buffer = buffer;
        }

        private RequestHead? _head = null;

        public bool NeedsData => _head is null;
        public bool HasHead => _head is not null;

        public RequestHead ReadHead()
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
            if (_instruction is null)
            {
                ProcessInstruction();
            }

            if (_instruction is not null)
            {
                ProcessHeaders();
            }
        }

        private void ProcessInstruction()
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

            _instruction = new Instruction(parts[0], parts[1], parts[2]);
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

                    if (_instruction is null)
                        throw new InvalidOperationException("Head termination received before instruction line");

                    _head = new RequestHead(
                        _instruction.Verb,
                        _instruction.Path,
                        _instruction.Version,
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
