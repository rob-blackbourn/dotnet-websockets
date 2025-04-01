using System;
using System.Collections.Generic;
using System.Text;

namespace WebSockets.Core.Http
{
    /// <summary>
    /// An HTTP request.
    /// </summary>
    public class HttpRequest
    {
        /// <summary>
        /// Construct an HTTP request.
        /// </summary>
        /// <param name="verb">The verb (GET, POST, etc).</param>
        /// <param name="path">The server path.</param>
        /// <param name="version">The HTTP version.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="body">An optional body.</param>
        public HttpRequest(
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

        public static HttpRequest Parse(byte[] data)
        {
            var reader = new HttpRequestReader();
            reader.WriteData(data, 0, data.Length);
            if (!reader.HasRequest)
                throw new InvalidOperationException("Failed to parse request");
            return reader.ReadRequest();
        }

        public byte[] ToBytes()
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

            var text = builder.ToString();
            var header = Encoding.ASCII.GetBytes(text);
            if (Body is null || Body.Length == 0)
                return header;

            var data = new byte[header.Length + Body.Length];
            Array.Copy(header, 0, data, 0, header.Length);
            Array.Copy(Body, 0, data, header.Length, Body.Length);

            return data;
        }

        public static HttpRequest CreateUpgradeRequest(
            string path,
            string host,
            string origin,
            string key,
            string[]? subProtocols)
        {
            var webRequest = new HttpRequest(
                "GET",
                path,
                "HTTP/1.1",
                new Dictionary<string, IList<string>>(StringComparer.InvariantCultureIgnoreCase)
                {
                    {"Host", [host] },
                    {"Upgrade", ["websocket"] },
                    {"Connection", ["Upgrade"] },
                    {"Origin", [origin] },
                    {"Sec-WebSocket-Version", ["13"] },
                    {"Sec-WebSocket-Key", [key] },
                },
                null
            );
            if (subProtocols is not null && subProtocols.Length > 0)
                webRequest.Headers.Add(
                    "Sec-WebSocket-Protocol",
                    [string.Join(',', subProtocols)]
                );

            return webRequest;
        }
    }
}