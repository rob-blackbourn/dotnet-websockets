using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WebSockets.Core
{
    /// <summary>
    /// A class modelling the required values of a WebSocket HTTP request.
    /// </summary>
    public class WebRequest
    {
        private static byte[] HTTP_EOM = "\r\n\r\n"u8.ToArray();

        /// <summary>
        /// Construct a web request.
        /// </summary>
        /// <param name="verb">The verb (GET, POST, etc).</param>
        /// <param name="path">The server path.</param>
        /// <param name="version">The HTTP version.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="body">An optional body.</param>
        public WebRequest(
            string verb,
            string path,
            string version,
            IDictionary<string, IList<string>> headers,
            byte[]? body)
        {
            Verb = verb;
            Path = path;
            Version = version;
            Headers = headers;
            Body = body;
        }

        /// <summary>
        /// The HTTP verb.
        /// </summary>
        /// <value>The verb.</value>
        public string Verb { get; private set; }
        /// <summary>
        /// The server path.
        /// </summary>
        /// <value>The path for the server.</value>
        public string Path { get; private set; }
        /// <summary>
        /// The HTTP version.
        /// </summary>
        /// <value>The version.</value>
        public string Version { get; private set; }
        /// <summary>
        /// The HTTP Headers.
        /// 
        /// Note: header keys are case insensitive.
        /// </summary>
        /// <value>The headers.</value>
        public IDictionary<string, IList<string>> Headers { get; private set; }
        public byte[]? Body { get; }

        public static WebRequest Parse(byte[] data)
        {
            var index = data.IndexOf(HTTP_EOM);
            if (index == -1)
                throw new ArgumentOutOfRangeException("Expected header terminator");

            var header = Encoding.UTF8.GetString(data.SubArray(0, index + 2));
            var body = data.Length == index + 4
                ? null
                : data.SubArray(index + 4);

            var lines = header.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            var (verb, path, version) = ParseRequestLine(lines[0]);
            var headers = ParseHeaderLines(lines.Skip(1));
            return new WebRequest(verb, path, version, headers, body);
        }

        private static IDictionary<string, IList<string>> ParseHeaderLines(IEnumerable<string> lines)
        {
            var headers = new Dictionary<string, IList<string>>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var line in lines)
            {
                var (name, value) = ParseHeaderLine(line);
                if (!headers.TryGetValue(name, out var list))
                    headers.Add(name, list = new List<string>());
                list.Add(value);
            }
            return headers;
        }

        private static (string, string, string) ParseRequestLine(string requestLine)
        {
            var parts = requestLine.Split(' ');
            if (parts.Length != 3)
                throw new InvalidDataException("The request line should have three parts separated by spaces");
            return (parts[0], parts[1], parts[2]);
        }

        private static (string, string) ParseHeaderLine(string headerLine)
        {
            var index = headerLine.IndexOf(':');
            if (index == -1)
                throw new InvalidDataException("A header line should contain a colon");
            var name = headerLine.Substring(0, index).Trim();
            var value = headerLine.Substring(index + 1).Trim();
            return (name.ToLowerInvariant(), value);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.AppendFormat("{0} {1} {2}\r\n", Verb, Path, Version);
            foreach (var (key, values) in Headers)
            {
                foreach (var value in values)
                {
                    builder.AppendFormat("{0}: {1}\r\n", key, value);
                }
            }
            builder.Append("\r\n");

            return builder.ToString();
        }

        public static WebRequest CreateUpgradeRequest(string path, string host, string origin, string key, string[]? subProtocols)
        {
            var webRequest = new WebRequest(
                "GET",
                path,
                "HTTP/1.1",
                new Dictionary<string, IList<string>>
                {
                    {"Host", new List<string> { host }},
                    {"Upgrade", new List<string> { "websocket" }},
                    {"Connection", new List<string> { "Upgrade" }},
                    {"Origin", new List<string> { origin }},
                    {"Sec-WebSocket-Version", new List<string> { "13" }},
                    {"Sec-WebSocket-Key", new List<string> { key }},
                },
                null
            );
            if (subProtocols is not null && subProtocols.Length > 0)
                webRequest.Headers.Add("Sec-WebSocket-Protocol", new List<string> { string.Join(',', subProtocols) });

            return webRequest;
        }
    }
}