using System;

namespace WebSockets.Core.Test
{
    [TestClass]
    public sealed class TestHandshake
    {
        [TestMethod]
        public void TestOpenHandshake()
        {
            var clientProtocol = new ClientHandshake(
                "gandalf.rivendell.com",
                ["foo", "bar"],
                new MockDateTimeProvider(new DateTime(2000, 1, 1, 15, 30, 0)),
                new MockNonceGenerator([91, 251, 225, 168], "x3JJHMbDL1EzLkh9GBhXDw=="));
            var serverProtocol = new ServerHandshake(
                ["bar"],
                new MockDateTimeProvider(new DateTime(2000, 1, 1, 15, 30, 1))
            );

            clientProtocol.WriteHandshakeRequest("/chat", "www.mordor.com");

            var buffer = new byte[1024];
            var isDone = false;
            while (!isDone)
            {
                var bytesRead = 0L;
                clientProtocol.ReadHandshakeData(buffer, ref bytesRead, buffer.LongLength);
                if (bytesRead == 0)
                    isDone = true;
                else
                    serverProtocol.WriteHandshakeData(buffer, 0, (int)bytesRead);
            }

            var webRequest = serverProtocol.ReadHandshakeRequest();
            Assert.IsNotNull(webRequest);
            serverProtocol.WriteHandshakeResponse(webRequest);

            isDone = false;
            while (!isDone)
            {
                var bytesRead = 0L;
                serverProtocol.ReadHandshakeData(buffer, ref bytesRead, buffer.LongLength);
                if (bytesRead == 0)
                    isDone = true;
                else
                    clientProtocol.WriteHandshakeData(buffer, 0, (int)bytesRead);
            }

            var webResponse = clientProtocol.ReadHandshakeResponse();
            Assert.IsNotNull(webResponse);
            Assert.AreEqual(HandshakeState.Succeeded, clientProtocol.State);
            Assert.AreEqual(HandshakeState.Succeeded, serverProtocol.State);
            Assert.AreEqual("bar", clientProtocol.SelectedSubProtocol);
            Assert.AreEqual("bar", serverProtocol.SelectedSubProtocol);
        }

        [TestMethod]
        public void TestOpenHandshakeReject()
        {
            var clientProtocol = new ClientHandshake(
                "gandalf.rivendell.com",
                ["foo", "bar"],
                new MockDateTimeProvider(new DateTime(2000, 1, 1, 15, 30, 0)),
                new MockNonceGenerator([91, 251, 225, 168], "x3JJHMbDL1EzLkh9GBhXDw=="));
            var serverProtocol = new ServerHandshake(
                ["bar"],
                new MockDateTimeProvider(new DateTime(2000, 1, 1, 15, 30, 1))
            );

            clientProtocol.WriteHandshakeRequest("/chat", "www.mordor.com");

            var buffer = new byte[1024];
            var isDone = false;
            while (!isDone)
            {
                var bytesRead = 0L;
                clientProtocol.ReadHandshakeData(buffer, ref bytesRead, buffer.LongLength);
                if (bytesRead == 0)
                    isDone = true;
                else
                    serverProtocol.WriteHandshakeData(buffer, 0, (int)bytesRead);
            }

            var webRequest = serverProtocol.ReadHandshakeRequest();
            Assert.IsNotNull(webRequest);
            serverProtocol.WriteHandshakeRejectResponse("invalid path");

            isDone = false;
            while (!isDone)
            {
                var bytesRead = 0L;
                serverProtocol.ReadHandshakeData(buffer, ref bytesRead, buffer.LongLength);
                if (bytesRead == 0)
                    isDone = true;
                else
                    clientProtocol.WriteHandshakeData(buffer, 0, (int)bytesRead);
            }

            var webResponse = clientProtocol.ReadHandshakeResponse();
            Assert.IsNotNull(webResponse);
            Assert.AreEqual(HandshakeState.Failed, clientProtocol.State);
            Assert.AreEqual(HandshakeState.Failed, serverProtocol.State);
        }
    }
}

