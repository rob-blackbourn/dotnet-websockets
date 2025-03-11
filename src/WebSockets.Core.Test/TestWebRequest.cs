using System.Linq;
using System.Text;

namespace WebSockets.Core.Test
{
    [TestClass]
    public sealed class TestWebRequest
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
                "Origin: http://example.com\r\n";
            var webRequest = WebRequest.Parse(Encoding.UTF8.GetBytes(text));
            Assert.AreEqual("GET", webRequest.Verb);
            Assert.AreEqual("/chat", webRequest.Path);
            Assert.AreEqual("HTTP/1.1", webRequest.Version);
            Assert.AreEqual(webRequest.Headers.SingleValue("Host"), "server.example.com");
            Assert.AreEqual(webRequest.Headers.SingleValue("Upgrade"), "websocket");
            Assert.AreEqual(webRequest.Headers.SingleValue("Sec-WebSocket-Key"), "x3JJHMbDL1EzLkh9GBhXDw==");
            Assert.IsTrue(webRequest.Headers.SingleCommaValues("Sec-WebSocket-Protocol")?.SequenceEqual(["chat", "superchat"]));
            Assert.AreEqual(webRequest.Headers.SingleValue("Sec-WebSocket-Version"), "13");
            Assert.AreEqual(webRequest.Headers.SingleValue("Origin"), "http://example.com");
        }
    }
}

