using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WebSockets.Core
{
    public class WebResponse
    {
        // TODO: This is shared by WedRequest
        private static byte[] HTTP_EOM = "\r\n\r\n"u8.ToArray();

        public WebResponse(
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

        public static WebResponse Parse(byte[] data)
        {
            var index = data.IndexOf(HTTP_EOM);
            if (index == -1)
                throw new ArgumentOutOfRangeException("Expected header terminator");

            var header = Encoding.UTF8.GetString(data.SubArray(0, index + 2));
            var body = data.Length == index + 4 ? null : data.SubArray(index + 4);

            var lines = header.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            var (version, code, reason) = ParseResponseLine(lines[0]);
            var headers = ParseHeaderLines(lines.Skip(1));
            return new WebResponse(version, code, reason, headers, body);
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

        private static (string, int, string) ParseResponseLine(string requestLine)
        {
            var parts = requestLine.Split(' ', 3);
            if (parts.Length != 3)
                throw new InvalidDataException("The response line should have three parts separated by spaces");
            if (!int.TryParse(parts[1], out var code))
                throw new InvalidDataException("The response code should be an integer");
            return (parts[0], code, parts[2]);
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
    }
}