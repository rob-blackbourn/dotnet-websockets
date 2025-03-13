# Introduction

By separating IO from the implementation of a protocol
the code becomes much easier to test.

## The protocol

The [WebSocket protocol](https://datatracker.ietf.org/doc/html/rfc6455)
has two major parts: the handshake and the messages.
Within the message protocol there are two further parts: the ping-pong exchange
and the close handshake.

### The Handshake

In the handshake the client connects to the server and sends an HTTP request.
The server produces an appropriate HTTP response, and all subsequent communication
uses the message protocol.

### The Message Protocol

The message protocol is a binary protocol which involves sending frames. A
frame has a type, a length, and a marker to indicate if this is the final
frame in the message.

#### Ping-Pong

The ping-pong exchange happens when one party sends a ping. The other party
is expected to respond with a pong with the same payload.

While the messages are supported, this state transition si not modelled
by the library.

#### Close Handshake

While either side may simply drop the connection, a close handshake is specified.
The handshake allows partially sent packages too be completely sent if the other
side supports this.

When a close is received by either party, the other is expected to send a close
with the same code (if any). After the handshake all data will discarded.

## The implementation

Both the handshake and message protocol follow the same pattern for getting
data from the network into and out of the protocol.
