using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WebSockets.Core
{
    public class WebResponse
    {
        public WebResponse(string version, int code, string reason, IDictionary<string, IList<string>> headers)
        {
            Version = version;
            Code = code;
            Reason = reason;
            Headers = headers;
        }

        public string Version { get; private set; }
        public int Code { get; private set; }
        public string Reason { get; private set; }
        public IDictionary<string, IList<string>> Headers { get; private set; }

        public static WebResponse Parse(string data)
        {
            /*
HTTP/1.1 101 Switching Protocols
Upgrade: websocket
Connection: Upgrade
Sec-WebSocket-Accept: HSmrc0sMlYUkAGmm5OPpG2HaGWk=
Sec-WebSocket-Protocol: chat            */
            var lines = data.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            var (version, code, reason) = ParseResponseLine(lines[0]);
            var headers = ParseHeaderLines(lines.Skip(1));
            return new WebResponse(version, code, reason, headers);
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
            var value = headerLine.Substring(index+1).Trim();
            return (name.ToLowerInvariant(), value);
        }
    }
}