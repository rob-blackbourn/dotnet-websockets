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
            var webresponse = WebResponse.Parse(text);
            Assert.AreEqual("HTTP/1.1", webresponse.Version);
            Assert.AreEqual(101, webresponse.Code);
            Assert.AreEqual("Switching Protocols", webresponse.Reason);
            Assert.AreEqual(webresponse.Headers.SingleValue("Connection"), "Upgrade");
            Assert.AreEqual(webresponse.Headers.SingleValue("Sec-WebSocket-Accept"), "HSmrc0sMlYUkAGmm5OPpG2HaGWk=");
            Assert.AreEqual(webresponse.Headers.SingleValue("Sec-WebSocket-Protocol"), "chat");
        }
    }
}

