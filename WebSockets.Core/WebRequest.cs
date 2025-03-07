using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WebSockets.Core
{
    public class WebRequest
    {
        public WebRequest(string verb, string path, string version, IDictionary<string, IList<string>> headers)
        {
            Verb = verb;
            Path = path;
            Version = version;
            Headers = headers;
        }

        public string Verb { get; private set; }
        public string Path { get; private set; }
        public string Version { get; private set; }
        public IDictionary<string, IList<string>> Headers { get; private set; }

        public static WebRequest Parse(string data)
        {
            var lines = data.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            var (verb, path, version) = ParseRequestLine(lines[0]);
            var headers = ParseHeaderLines(lines.Skip(1));
            return new WebRequest(verb, path, version, headers);
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
    }
}