using System.Collections.Generic;

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
}
