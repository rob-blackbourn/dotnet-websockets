using System;
using System.Collections.Generic;
using System.Text;

namespace WebSockets.Core.Http
{
    public class Response
    {
        public Response(
            string version,
            int code,
            string reason,
            IDictionary<string, IList<string>> headers,
            byte[]? body)
        {
            Version = version;
            Code = code;
            Reason = reason;
            Headers = headers;
            Body = body;
        }

        public string Version { get; private set; }
        public int Code { get; private set; }
        public string Reason { get; private set; }
        public IDictionary<string, IList<string>> Headers { get; private set; }
        public byte[]? Body { get; }

        public static Response Parse(byte[] data)
        {
            var parser = new ResponseParser();
            parser.WriteData(data, 0, data.LongLength);
            if (!parser.HasResponse)
                throw new InvalidOperationException("Failed to parse response");
            return parser.ReadResponse();
        }

        public byte[] ToBytes()
        {
            var builder = new StringBuilder();

            builder.AppendFormat("{0} {1} {2}\r\n", Version, Code, Reason);
            foreach (var (key, values) in Headers)
            {
                foreach (var value in values)
                {
                    builder.AppendFormat("{0}: {1}\r\n", key, value);
                }
            }
            if (Body is not null && Body.Length > 0)
                builder.AppendFormat("Content-Length: {0}\r\n", Body.Length);

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

        public static Response CreateAcceptResponse(string responseKey, string? subProtocol)
        {
            var webResponse = new Response(
                "HTTP/1.1",
                101,
                "Switching Protocols",
                new Dictionary<string, IList<string>>
                {
                    {"Upgrade", new List<string> { "websocket" }},
                    {"Connection", new List<string> { "upgrade" }},
                    {"Sec-WebSocket-Accept", new List<string> { responseKey }},
                },
                null
            );
            if (subProtocol is not null)
                webResponse.Headers.Add("Sec-WebSocket-Protocol", new List<string> { subProtocol });

            return webResponse;
        }

        public static Response CreateErrorResponse(string reason, DateTime date)
        {
            var webResponse = new Response(
                "HTTP/1.1",
                400,
                "Bad Request",
                new Dictionary<string, IList<string>>
                {
                    { "Date", new List<string> { date.ToUniversalTime().ToString("r") }},
                    { "Connection", new List<string> { "close" }},
                    { "Content-Type", new List<string> { "text/plain; charset=utf-8" }},
                },
                Encoding.UTF8.GetBytes(reason)
            );
            return webResponse;
        }
    }
}