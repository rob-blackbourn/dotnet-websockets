using WebSockets.Core.Messages;

namespace WebSockets.Core.Test
{
    [TestClass]
    public sealed class TestMessageProtocol
    {
        [TestMethod]
        public void TestServerClose()
        {
            var data = new byte[1024];
            long bytesRead;

            var serverProtocol = new MessageProtocol(false);
            var clientProtocol = new MessageProtocol(true);

            // Server send close.
            serverProtocol.WriteMessage(new CloseMessage(1000, "Normal closure"));
            Assert.IsTrue(serverProtocol.HasData);
            bytesRead = serverProtocol.ReadData(data, 0, data.Length);
            Assert.IsFalse(serverProtocol.HasData);

            // Client receive close.
            clientProtocol.WriteData(data, 0, bytesRead);
            Assert.IsTrue(clientProtocol.HasMessage);
            var serverMessage = clientProtocol.ReadMessage();
            Assert.AreEqual(MessageType.Close, serverMessage.Type);
            var serverCloseMessage = (CloseMessage)serverMessage;
            Assert.IsFalse(clientProtocol.HasMessage);

            // Client send close.
            clientProtocol.WriteMessage(new CloseMessage(serverCloseMessage.Code, null));
            Assert.IsTrue(clientProtocol.HasData);
            bytesRead = clientProtocol.ReadData(data, 0, data.Length);
            Assert.IsFalse(clientProtocol.HasData);

            // Server receive close.
            serverProtocol.WriteData(data, 0, bytesRead);
            Assert.IsTrue(serverProtocol.HasMessage);
            var responseMessage = serverProtocol.ReadMessage();
            Assert.AreEqual(MessageType.Close, responseMessage.Type);
            Assert.IsTrue(1000 == ((CloseMessage)responseMessage).Code);
        }
    }
}