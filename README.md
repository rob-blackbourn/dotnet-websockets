# dotnet-websockets

A sans-io implementation of WebSockets in .Net.

## Status

I believe this is working. Please report any issues.

## The API

The API is actually very simple, but the read/write semantics can be confusing.

At the heart of a sans-io implementation is a processing engine; which
is known here as the "protocol". When bytes are "read" from the network,
they are "written" to the protocol. the protocol decodes the bytes to
higher level objects like web requests and websocket messages. Some
business logic is applied to these higher level objects, which produce
other higher level objects (like web responses or new websocket messages).
These objects are then written to the protocol. Finally bytes are read
from the protocol which are then written to the network.

To Summarize there is a read flow and a write flow:

### The read flow

* Read bytes from network
* Write bytes to protocol
* Read objects from protocol

### The write flow

* Write objects to protocol
* Read bytes from protocol
* Write bytes to network.

## Web Handshake, Messages, and Closing

A WebSocket goes through two protocol lifecycle stages.

* Connection Handshake
* Messages

The connection handshake takes a web request and attempts to negotiate a websocket connection.
This statement suggests a more complicated workflow than actually exists.
What happens here is the client sends a standard "GET" web request with some headers that indicate a websocket is requested.
If the server supports websockets a response is sent, and all subsequent communication uses the websocket protocol.

### Data Messages

Once the protocol is established, data messages can be sent between the client and the server.
These messages can be either text, or binary.

### Control Messages

As well as data messages, there are some control messages that can be sent.

* Ping/Pong
* Close

#### Ping/Pong

If a client receives a ping it is expected to respond with a pong with the same data received.

#### Close

While either participant can simply close the connection, the protocol specifies a close handshake.
The handshake consists of one side sending a close message with an optional code and text reason,
where the other side responds with a close containing the same data.

## The Implementation

There are two main parts to the implementation: The protocols and the messages.

The messages provide a way to represent the information exchanged between the clients
and the servers. The protocols provide the mechanism by which these messages are passed.

### The Protocol

The protocol base class provides the following methods:

* ReadHandshakeData
* WriteHandshakeData
* ReadMessageData
* WriteMessageData
* ReadMessage
* WriteMessage

#### The ServerProtocol

The server protocol provides the extra method:

* ProcessHandshakeRequest

#### The ClientProtocol

The client protocol provides the extra methods:

* SendHandshakeRequest
* ProcessHandshakeResponse

## Examples

There is an example echo server written for both the client and the server.
