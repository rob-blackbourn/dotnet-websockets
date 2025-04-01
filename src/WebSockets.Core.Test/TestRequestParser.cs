using System.Linq;
using System.Text;

using WebSockets.Core.Http;

namespace WebSockets.Core.Test
{
    [TestClass]
    public sealed class TestRequestParser
    {
        [TestMethod]
        public void TestParseHead()
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

        [TestMethod]
        public void TestParseChunkedBody()
        {
            var data = Encoding.UTF8.GetBytes(
                "4\r\nWiki\r\n7\r\npedia i\r\nB\r\nn \r\nchunks.\r\n0\r\n\r\n"
            );
            var buffer = new FragmentBuffer<byte>();
            var parser = new ChunkedBodyReader(buffer);
            Assert.IsTrue(parser.NeedsData);
            parser.WriteData(data, 0, data.LongLength);
            Assert.IsFalse(parser.NeedsData);
            Assert.IsTrue(parser.HasBody);
            var body = parser.ReadBody();
            var text = Encoding.UTF8.GetString(body);
            Assert.AreEqual("Wikipedia in \r\nchunks.", text);
        }

        [TestMethod]
        public void TestParseFixedLengthBody()
        {
            var data = Encoding.UTF8.GetBytes(
                "Wikipedia in \r\nchunks."
            );
            var buffer = new FragmentBuffer<byte>();
            var parser = new HttpFixedLengthBodyReader(data.Length, buffer);
            Assert.IsTrue(parser.NeedsData);
            parser.WriteData(data, 0, data.LongLength);
            Assert.IsFalse(parser.NeedsData);
            Assert.IsTrue(parser.HasBody);
            var body = parser.ReadBody();
            var text = Encoding.UTF8.GetString(body);
            Assert.AreEqual("Wikipedia in \r\nchunks.", text);
        }

        [TestMethod]
        public void TestParseRequestWithNoBody()
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
            var parser = new HttpRequestReader();
            Assert.IsTrue(parser.NeedsData);
            parser.WriteData(data, 0, data.LongLength);
            Assert.IsFalse(parser.NeedsData);
            Assert.IsTrue(parser.HasRequest);
            var request = parser.ReadRequest();

            Assert.AreEqual("GET", request.Verb);
            Assert.AreEqual("/chat", request.Path);
            Assert.AreEqual("HTTP/1.1", request.Version);
            Assert.AreEqual(request.Headers.SingleValue("Host"), "server.example.com");
            Assert.AreEqual(request.Headers.SingleValue("Upgrade"), "websocket");
            Assert.AreEqual(request.Headers.SingleValue("Sec-WebSocket-Key"), "x3JJHMbDL1EzLkh9GBhXDw==");
            Assert.IsTrue(request.Headers.SingleCommaValues("Sec-WebSocket-Protocol")?.SequenceEqual(["chat", "superchat"]));
            Assert.AreEqual(request.Headers.SingleValue("Sec-WebSocket-Version"), "13");
            Assert.AreEqual(request.Headers.SingleValue("Origin"), "http://example.com");
        }

        [TestMethod]
        public void TestParseRequestWithChunkedBody()
        {
            var text =
                "GET /chat HTTP/1.1\r\n" +
                "Host: server.example.com\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Transfer-Encoding: chunked\r\n" +
                "Sec-WebSocket-Key: x3JJHMbDL1EzLkh9GBhXDw==\r\n" +
                "Sec-WebSocket-Protocol: chat, superchat\r\n" +
                "Sec-WebSocket-Version: 13\r\n" +
                "Origin: http://example.com\r\n" +
                "\r\n" +
                "4\r\nWiki\r\n" +
                "7\r\npedia i\r\n" +
                "B\r\nn \r\nchunks.\r\n" +
                "0\r\n" +
                "\r\n";
            var data = Encoding.UTF8.GetBytes(text);
            var parser = new HttpRequestReader();
            Assert.IsTrue(parser.NeedsData);
            parser.WriteData(data, 0, data.LongLength);
            Assert.IsFalse(parser.NeedsData);
            Assert.IsTrue(parser.HasRequest);
            var request = parser.ReadRequest();

            Assert.AreEqual("GET", request.Verb);
            Assert.AreEqual("/chat", request.Path);
            Assert.AreEqual("HTTP/1.1", request.Version);
            Assert.AreEqual(request.Headers.SingleValue("Host"), "server.example.com");
            Assert.AreEqual(request.Headers.SingleValue("Upgrade"), "websocket");
            Assert.AreEqual(request.Headers.SingleValue("Sec-WebSocket-Key"), "x3JJHMbDL1EzLkh9GBhXDw==");
            Assert.IsTrue(request.Headers.SingleCommaValues("Sec-WebSocket-Protocol")?.SequenceEqual(["chat", "superchat"]));
            Assert.AreEqual(request.Headers.SingleValue("Sec-WebSocket-Version"), "13");
            Assert.AreEqual(request.Headers.SingleValue("Origin"), "http://example.com");

            Assert.IsNotNull(request.Body);
            var body = Encoding.UTF8.GetString(request.Body);
            Assert.AreEqual("Wikipedia in \r\nchunks.", body);
        }

        [TestMethod]
        public void TestParseRequestWithFixedLengthBody()
        {
            var body = "This is not a test";
            var bodyLength = Encoding.UTF8.GetByteCount("This is not a test");
            var text =
                "GET /chat HTTP/1.1\r\n" +
                "Host: server.example.com\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                $"Content-Length: {bodyLength}\r\n" +
                "Sec-WebSocket-Key: x3JJHMbDL1EzLkh9GBhXDw==\r\n" +
                "Sec-WebSocket-Protocol: chat, superchat\r\n" +
                "Sec-WebSocket-Version: 13\r\n" +
                "Origin: http://example.com\r\n" +
                "\r\n" +
                body;
            var data = Encoding.UTF8.GetBytes(text);
            var parser = new HttpRequestReader();
            Assert.IsTrue(parser.NeedsData);
            parser.WriteData(data, 0, data.LongLength);
            Assert.IsFalse(parser.NeedsData);
            Assert.IsTrue(parser.HasRequest);
            var request = parser.ReadRequest();

            Assert.AreEqual("GET", request.Verb);
            Assert.AreEqual("/chat", request.Path);
            Assert.AreEqual("HTTP/1.1", request.Version);
            Assert.AreEqual(request.Headers.SingleValue("Host"), "server.example.com");
            Assert.AreEqual(request.Headers.SingleValue("Upgrade"), "websocket");
            Assert.AreEqual(request.Headers.SingleValue("Sec-WebSocket-Key"), "x3JJHMbDL1EzLkh9GBhXDw==");
            Assert.IsTrue(request.Headers.SingleCommaValues("Sec-WebSocket-Protocol")?.SequenceEqual(["chat", "superchat"]));
            Assert.AreEqual(request.Headers.SingleValue("Sec-WebSocket-Version"), "13");
            Assert.AreEqual(request.Headers.SingleValue("Origin"), "http://example.com");

            Assert.IsNotNull(request.Body);
            var payload = Encoding.UTF8.GetString(request.Body);
            Assert.AreEqual(body, payload);
        }
    }
}

