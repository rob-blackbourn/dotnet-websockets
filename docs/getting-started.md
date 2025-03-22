# Getting Started

There is an example echo server and client in the `src/Examples` folder
which demonstrates how to use IO with this library.

The code here assumes the client and server have connected over tcp, and have
each acquired a `Stream`.

## The Handshake

The client starts the handshake by sending a web request.

```csharp
using System;
using System.IO;

using WebSockets.Core;

namespace EchoClient
{
    class Connection
    {
        private readonly Stream _stream;
        private readonly ClientHandshakeProtocol _handshakeProtocol;

        ...

        private void SendHandshakeRequest()
        {
            Console.WriteLine("Sending handshake request");
            _handshakeProtocol.WriteRequest("/chat", "www.example.com");

            var buffer = new byte[1024];
            var isDone = false;
            while (!isDone)
            {
                var bytesRead = 0L;
                _handshakeProtocol.ReadData(buffer, ref bytesRead, buffer.LongLength);
                if (bytesRead == 0)
                    isDone = true;
                else
                    _stream.Write(buffer, 0, (int)bytesRead);
            }
        }

```

The server receives the request and returns a result.

```csharp
namespace EchoServer
{
    class Connection
    {
        private readonly Stream _stream;
        private readonly ServerHandshakeProtocol _handshakeProtocol;

        private void ReceiveHandshakeRequest()
        {
            Console.WriteLine("Receiving handshake request");

            WebRequest? webRequest = null;
            var buffer = new byte[1024];
            while (webRequest is null)
            {
                var bytesRead = _stream.Read(buffer);
                if (bytesRead == 0)
                    throw new EndOfStreamException();
                _handshakeProtocol.WriteData(buffer, 0, bytesRead);

                webRequest = _handshakeProtocol.ReadRequest();
            }

            var webResponse = _handshakeProtocol.CreateWebResponse(webRequest);

            _handshakeProtocol.WriteResponse(webResponse);
        }

        private void SendHandshakeResponse()
        {
            Console.WriteLine("Sending handshake response");

            bool isDone = false;
            while (!isDone)
            {
                var buffer = new byte[1024];
                var bytesRead = 0L;
                _handshakeProtocol.ReadData(buffer, ref bytesRead, buffer.LongLength);
                if (bytesRead == 0)
                    isDone = true;
                else
                {
                    _stream.Write(buffer, 0, (int)bytesRead);
                    Console.WriteLine("Sent client data");
                }
            }
        }
    }
}
```

The client receives the server's response.

```csharp
using System;
using System.IO;
using System.Net.Sockets;

using WebSockets.Core;
using WebSockets.Core.Messages;

namespace EchoClient
{
    class Connection
    {
        private readonly Stream _stream;
        private readonly ClientHandshakeProtocol _handshakeProtocol;

        ...

        private WebResponse? ReceiveHandshakeResponse()
        {
            Console.WriteLine("Receiving handshake response");
            var buffer = new byte[1024];
            var offset = 0L;
            var isDone = false;
            while (!isDone)
            {
                var bytesRead = _stream.Read(buffer);
                _handshakeProtocol.WriteData(
                    buffer,
                    offset,
                    bytesRead);
                if (offset == bytesRead)
                    offset = 0;
                isDone = _handshakeProtocol.ReadResponse() is not null;
            }

            return _handshakeProtocol.ReadResponse();
        }
    }
}
```
