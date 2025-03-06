using System.Linq;
using System.Text;
using WebSockets.Core;

namespace WebSockets.Core.Test
{
    [TestClass]
    public sealed class TestWebResponse
    {
        [TestMethod]
        public void TestParse()
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
    }
}

