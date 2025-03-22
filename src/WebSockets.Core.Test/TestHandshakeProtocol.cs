using System;

namespace WebSockets.Core.Test
{
    [TestClass]
    public sealed class TestHandshakeProtocol
    {
        [TestMethod]
        public void TestOpenHandshake()
        {
            var clientProtocol = new ClientHandshakeProtocol(
                "gandalf.rivendell.com",
                ["foo", "bar"],
                new MockDateTimeProvider(new DateTime(2000, 1, 1, 15, 30, 0)),
                new MockNonceGenerator([91, 251, 225, 168], "x3JJHMbDL1EzLkh9GBhXDw=="));
            var serverProtocol = new ServerHandshakeProtocol(
                ["bar"],
                new MockDateTimeProvider(new DateTime(2000, 1, 1, 15, 30, 1))
            );

            clientProtocol.WriteRequest("/chat", "www.mordor.com");

            var buffer = new byte[1024];
            while (true)
            {
                var bytesRead = clientProtocol.ReadData(buffer);
                if (bytesRead == 0)
                    break;

                serverProtocol.WriteData(buffer, 0, bytesRead);
            }

            var webRequest = serverProtocol.ReadRequest();
            Assert.IsNotNull(webRequest);
            serverProtocol.WriteResponse(serverProtocol.CreateWebResponse(webRequest));

            while (true)
            {
                var bytesRead = serverProtocol.ReadData(buffer);
                if (bytesRead == 0)
                    break;

                clientProtocol.WriteData(buffer, 0, bytesRead);
            }

            var webResponse = clientProtocol.ReadResponse();
            Assert.IsNotNull(webResponse);
            Assert.AreEqual(HandshakeProtocolState.Succeeded, clientProtocol.State);
            Assert.AreEqual(HandshakeProtocolState.Succeeded, serverProtocol.State);
            Assert.AreEqual("bar", clientProtocol.SelectedSubProtocol);
            Assert.AreEqual("bar", serverProtocol.SelectedSubProtocol);
        }

        [TestMethod]
        public void TestOpenHandshakeReject()
        {
            var dateTimeProvider = new MockDateTimeProvider(new DateTime(2000, 1, 1, 15, 30, 0));
            var nonceGenerator = new MockNonceGenerator([91, 251, 225, 168], "x3JJHMbDL1EzLkh9GBhXDw==");

            var clientProtocol = new ClientHandshakeProtocol(
                "gandalf.rivendell.com",
                ["foo", "bar"],
                dateTimeProvider,
                nonceGenerator);
            var serverProtocol = new ServerHandshakeProtocol(
                ["bar"],
                dateTimeProvider
            );

            clientProtocol.WriteRequest("/chat", "www.mordor.com");

            var buffer = new byte[1024];
            while (true)
            {
                var bytesRead = clientProtocol.ReadData(buffer);
                if (bytesRead == 0)
                    break;

                serverProtocol.WriteData(buffer, 0, bytesRead);
            }

            var webRequest = serverProtocol.ReadRequest();
            Assert.IsNotNull(webRequest);
            serverProtocol.WriteResponse(WebResponse.CreateErrorResponse("invalid path", dateTimeProvider.Now));

            while (true)
            {
                var bytesRead = serverProtocol.ReadData(buffer);
                if (bytesRead == 0)
                    break;

                clientProtocol.WriteData(buffer, 0, bytesRead);
            }

            var webResponse = clientProtocol.ReadResponse();
            Assert.IsNotNull(webResponse);
            Assert.AreEqual(HandshakeProtocolState.Failed, clientProtocol.State);
            Assert.AreEqual(HandshakeProtocolState.Failed, serverProtocol.State);
        }
    }
}

