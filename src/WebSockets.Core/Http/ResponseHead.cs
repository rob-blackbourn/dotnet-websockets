using System.Collections.Generic;

namespace WebSockets.Core.Http
{
    class ResponseHead
    {
        public ResponseHead(
            string version,
            int code,
            string reason,
            IDictionary<string, IList<string>> headers)
        {
            Version = version;
            Code = code;
            Reason = reason;
            Headers = headers;
        }

        public string Version { get; }
        public int Code { get; }
        public string Reason { get; }
        public IDictionary<string, IList<string>> Headers { get; set; }
    }
}
