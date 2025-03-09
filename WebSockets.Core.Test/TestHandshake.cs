using System;

namespace WebSockets.Core.Test
{
    [TestClass]
    public sealed class TestHandshake
    {
        [TestMethod]
        public void TestOpenHandshake()
        {
            var clientProtocol = new ClientProtocol(
                "gandalf.rivendell.com",
                ["foo", "bar"],
                new MockDateTimeProvider(new DateTime(2000, 1, 1, 15, 30, 0)),
                new MockNonceGenerator([91, 251, 225, 168], "x3JJHMbDL1EzLkh9GBhXDw=="));
            var serverProtocol = new ServerProtocol(
                ["bar"],
                new MockDateTimeProvider(new DateTime(2000, 1, 1, 15, 30, 1)),
                new MockNonceGenerator([91, 251,255, 168], "x3JJHMbDL1EzLkh9GBhXDw==")
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
            Assert.AreEqual(ConnectionState.Connected, clientProtocol.State);
            Assert.AreEqual(ConnectionState.Connected, serverProtocol.State);
            Assert.AreEqual("bar", clientProtocol.SelectedSubProtocol);
            Assert.AreEqual("bar", serverProtocol.SelectedSubProtocol);
        }

        [TestMethod]
        public void TestOpenHandshakeReject()
        {
            var clientProtocol = new ClientProtocol(
                "gandalf.rivendell.com",
                ["foo", "bar"],
                new MockDateTimeProvider(new DateTime(2000, 1, 1, 15, 30, 0)),
                new MockNonceGenerator([91, 251, 225, 168], "x3JJHMbDL1EzLkh9GBhXDw=="));
            var serverProtocol = new ServerProtocol(
                ["bar"],
                new MockDateTimeProvider(new DateTime(2000, 1, 1, 15, 30, 1)),
                new MockNonceGenerator([91, 251,255, 168], "x3JJHMbDL1EzLkh9GBhXDw==")
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
            Assert.AreEqual(ConnectionState.Faulted, clientProtocol.State);
            Assert.AreEqual(ConnectionState.Faulted, serverProtocol.State);
        }
    }
}

