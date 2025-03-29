using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WebSockets.Core.Http
{
    abstract class BodyParser
    {
        private readonly string _verb;
        private readonly string _path;
        private readonly string _version;
        private readonly IDictionary<string, IList<string>> _headers;

        protected BodyParser(
            string verb,
            string path,
            string version,
            IDictionary<string, IList<string>> headers)
        {
            _verb = verb;
            _path = path;
            _version = version;
            _headers = headers;
        }

        public Request ToRequest(byte[]? body)
        {
            return new Request(
                    _verb,
                    _path,
                    _version,
                    _headers,
                    body
                );
        }
    }
}
