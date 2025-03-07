using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebSockets.Core;

namespace WebSockets.Core.Test
{
    [TestClass]
    public sealed class TestWebResponse
    {
        [TestMethod]
        public void TestParseWebSocketRequest()
        {
            var text =
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Accept: HSmrc0sMlYUkAGmm5OPpG2HaGWk=\r\n" +
                "Sec-WebSocket-Protocol: chat\r\n" +
                "\r\n";
            var data = Encoding.UTF8.GetBytes(text);
            var webResponse = WebResponse.Parse(data);
            Assert.AreEqual("HTTP/1.1", webResponse.Version);
            Assert.AreEqual(101, webResponse.Code);
            Assert.AreEqual("Switching Protocols", webResponse.Reason);
            Assert.AreEqual(webResponse.Headers.SingleValue("Connection"), "Upgrade");
            Assert.AreEqual(webResponse.Headers.SingleValue("Sec-WebSocket-Accept"), "HSmrc0sMlYUkAGmm5OPpG2HaGWk=");
            Assert.AreEqual(webResponse.Headers.SingleValue("Sec-WebSocket-Protocol"), "chat");
        }

        [TestMethod]
        public void TestRoundTrip()
        {
            var webResponse = new WebResponse(
                "HTTP/1.1",
                101,
                "Switching Protocols",
                new Dictionary<string, IList<string>>
                {
                    { "Upgrade", new List<string> { "websocket" }},
                    { "Connection", new List<string> { "Upgrade" }},
                    { "Sec-WebSocket-Accept", new List<string> { "HSmrc0sMlYUkAGmm5OPpG2HaGWk=" }},
                    { "Sec-WebSocket-Protocol", new List<string> { "chat" }},
                },
                null
            );
            var data = webResponse.ToBytes();
            var actual = Encoding.UTF8.GetString(data);
            var expected =
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Accept: HSmrc0sMlYUkAGmm5OPpG2HaGWk=\r\n" +
                "Sec-WebSocket-Protocol: chat\r\n" +
                "\r\n";
            Assert.AreEqual(expected, actual);
        }
        [TestMethod]

        public void TestRoundTripWithBody()
        {
            var webResponse = new WebResponse(
                "HTTP/1.1",
                101,
                "Switching Protocols",
                new Dictionary<string, IList<string>>
                {
                    { "Upgrade", new List<string> { "websocket" }},
                    { "Connection", new List<string> { "Upgrade" }},
                    { "Sec-WebSocket-Accept", new List<string> { "HSmrc0sMlYUkAGmm5OPpG2HaGWk=" }},
                    { "Sec-WebSocket-Protocol", new List<string> { "chat" }},
                },
                Encoding.UTF8.GetBytes("This is not a test")
            );
            var data = webResponse.ToBytes();
            var actual = Encoding.UTF8.GetString(data);
            var expected =
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Accept: HSmrc0sMlYUkAGmm5OPpG2HaGWk=\r\n" +
                "Sec-WebSocket-Protocol: chat\r\n" +
                "Content-Length: 18\r\n" +
                "\r\n" +
                "This is not a test";
            Assert.AreEqual(expected, actual);
        }
    }
}

