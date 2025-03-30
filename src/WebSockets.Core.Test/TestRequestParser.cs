using System.Linq;
using System.Text;
using WebSockets.Core.Http;

namespace WebSockets.Core.Test
{
    [TestClass]
    public sealed class TestRequestParser
    {
        [TestMethod]
        public void TestParse()
        {
            var text =
                "GET /chat HTTP/1.1\r\n" +
                "Host: server.example.com\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Key: x3JJHMbDL1EzLkh9GBhXDw==\r\n" +
                "Sec-WebSocket-Protocol: chat, superchat\r\n" +
                "Sec-WebSocket-Version: 13\r\n" +
                "Origin: http://example.com\r\n" +
                "\r\n";
            var data = Encoding.UTF8.GetBytes(text);
            var buffer = new FragmentBuffer<byte>();
            var headParser = new HeadRequestParser(buffer);
            Assert.IsTrue(headParser.NeedsData);
            headParser.WriteData(data, 0, data.LongLength);
            Assert.IsFalse(headParser.NeedsData);
            Assert.IsTrue(headParser.HasHead);
            var head = headParser.ReadHead();

            Assert.AreEqual("GET", head.Verb);
            Assert.AreEqual("/chat", head.Path);
            Assert.AreEqual("HTTP/1.1", head.Version);
            Assert.AreEqual(head.Headers.SingleValue("Host"), "server.example.com");
            Assert.AreEqual(head.Headers.SingleValue("Upgrade"), "websocket");
            Assert.AreEqual(head.Headers.SingleValue("Sec-WebSocket-Key"), "x3JJHMbDL1EzLkh9GBhXDw==");
            Assert.IsTrue(head.Headers.SingleCommaValues("Sec-WebSocket-Protocol")?.SequenceEqual(["chat", "superchat"]));
            Assert.AreEqual(head.Headers.SingleValue("Sec-WebSocket-Version"), "13");
            Assert.AreEqual(head.Headers.SingleValue("Origin"), "http://example.com");
        }
    }
}

