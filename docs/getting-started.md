# Getting Started

There is an example echo server and client in the `src/Examples` folder
which demonstrates how to use IO with this library.

The code here assumes the client and server have connected over tcp, and have
each acquired a `Stream`.

## Handshake Protocol

### Handshake Protocol Overview

The server listens on a socket waiting to accept connections. When a client
connects it writes a web request to the server. The server reads the request,
and writes a response, which is finally read by the client.

To summarize.

* Client writes a web request to the server.
* Server reads the web request.
* Server writes a web response.
* Client reads the web response.

### Handshake Protocol Code

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

        private void WriteHandshakeRequest()
        {
            _handshakeProtocol.WriteRequest("/chat", "www.example.com");

            var buffer = new byte[1024];
            while (true)
            {
                var bytesRead = _handshakeProtocol.ReadData(buffer);
                if (bytesRead == 0)
                    break;

                _stream.Write(buffer, 0, (int)bytesRead);
            }
        }

        ...
    }
}

```

The server receives the request and returns a result.

```csharp
using System;
using System.IO;
using System.Net.Sockets;

using WebSockets.Core;

namespace EchoServer
{
    class Connection
    {
        private readonly Stream _stream;
        private readonly ServerHandshakeProtocol _handshakeProtocol;

        ...

        private void ReadHandshakeRequest()
        {
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

        private void WriteHandshakeResponse()
        {
            var buffer = new byte[1024];
            while (true)
            {
                var bytesRead = _handshakeProtocol.ReadData(buffer);
                if (bytesRead == 0)
                    break;

                _stream.Write(buffer, 0, (int)bytesRead);
            }
        }

        ...
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

        private WebResponse? ReadHandshakeResponse()
        {
            var buffer = new byte[1024];
            WebResponse? webResponse = null;
            while (webResponse is null)
            {
                var bytesRead = _stream.Read(buffer);
                _handshakeProtocol.WriteData(buffer, 0, bytesRead);
                webResponse = _handshakeProtocol.ReadResponse();
            }

            return webResponse;
        }

        ...

    }
}
```

## Message Protocol

### Message Protocol Overview

To write a message, the message is first written to the protocol. This is
serialize the message to bytes. These bytes are read from the protocol and
written to the network until the protocol no-longer has data to read.

To read a message, bytes are read from the network and written to the
protocol until the protocol has a complete message. The message can then be
read from the protocol.

### Message Protocol Code

The following writes a message.

```csharp
namespace EchoClientOrServer
{
    public class Connection
    {
        private void WriteMessage(Message message)
        {
            _messageProtocol.WriteMessage(message);

            var buffer = new byte[1024];
            while (_messageProtocol.HasData)
            {
                var offset = 0L;
                var bytesRead = _messageProtocol.ReadData(buffer, offset, buffer.Length);
                _stream.Write(buffer, 0, (int)bytesRead);
            }
        }
    }
}
```

The following reads a message.

```csharp
namespace EchoClientOrServer
{
    public class Connection
    {
        ...

        private Message ReadMessage()
        {
            var buffer = new byte[1024];
            while (true)
            {
                if (_messageProtocol.HasMessage)
                    return _messageProtocol.ReadMessage();

                var bytesRead = _stream.Read(buffer);
                if (bytesRead == 0)
                    throw new EndOfStreamException("The client closed the connection.");

                _messageProtocol.WriteData(buffer, 0, bytesRead);
            }
        }

        ...
    }
}
```
