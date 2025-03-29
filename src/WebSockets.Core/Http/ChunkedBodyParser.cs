using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSockets.Core.Http
{
    /// <summary>
    /// A class modelling the required values of a WebSocket HTTP request.
    /// 
    /// https://en.wikipedia.org/wiki/Chunked_transfer_encoding
    /// </summary>
    class ChunkedBodyParser : BodyParser
    {
        internal static byte[] EOL = "\r\n"u8.ToArray();
        internal static byte[] EOM = "\r\n\r\n"u8.ToArray();

        private readonly FragmentBuffer<byte> _buffer = new();
        private readonly FragmentBuffer<byte> _chunks = new();
        private int? _chunkLength = null;
        private Request? _request = null;

        public ChunkedBodyParser(
            string verb,
            string path,
            string version,
            IDictionary<string, IList<string>> headers)
            : base(verb, path, version, headers)
        {
        }

        public Request ReadRequest()
        {
            if (_request is null)
                throw new InvalidOperationException("Request not available");
            return _request;
        }

        public void WriteData(byte[] array, long offset, long length)
        {
            _buffer.Write(array, offset, length);

            ProcessData();
        }

        private void ProcessData()
        {
            // A chunk is an ascii encoded hexadecimal string number followed
            // by a CR/LF, followed by the number of bytes specified, followed
            // by CR/LF.
            // 
            if (!_chunkLength.HasValue)
            {
                var i = _buffer.IndexOf(EOL);

                if (i == -1)
                    return; // Not enough data

                if (i == 0)
                {
                    // The chunk has started with cr/lf
                    if (_chunks.Count > 0)
                        throw new InvalidOperationException(
                            "received cf/lf at start of stream while expecting the chunk length");

                    // Consume buffer.
                    _buffer.ToArray();
                    _request = ToRequest(null);
                    return;
                }

                // Read until the end of cr/lf.
                var buf = new byte[i + EOL.Length];
                _buffer.ReadExactly(buf);
                // Convert the hex string to the chunk length.
                var text = Encoding.ASCII.GetString(buf, 0, (int)i);
                _chunkLength = Convert.ToInt32(text, 16);
            }

            if (_chunkLength.HasValue)
            {
                if (_chunkLength.Value == 0)
                {
                    // The final chunk is of zero length.

                    // Expect cr/lf
                    if (_buffer.Count < EOL.Length)
                        return;

                    // Check the buffer contains the final cr/lf.
                    var buf = new byte[EOL.Length];
                    _buffer.ReadExactly(buf);
                    if (!buf.SequenceEqual(EOL))
                        throw new InvalidOperationException("Invalid termination of chunk stream");

                    // The buffer should now be empty.
                    if (_buffer.Count != 0)
                        throw new InvalidOperationException("Expected the buffer to be empty");

                    _request = ToRequest(_chunks.ToArray());
                }
                else
                {
                    // Expect the chunk length plus cr/lf.
                    if (_buffer.Count < _chunkLength.Value + EOL.Length)
                        return;

                    // Get the chunk.
                    var chunk = new byte[_chunkLength.Value];
                    _buffer.ReadExactly(chunk);
                    _chunks.Write(chunk);

                    // Clear the length to get the next.
                    _chunkLength = null;
                }
            }
        }
    }
}
