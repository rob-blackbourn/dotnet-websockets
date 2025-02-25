using System;
using System.Linq;
using System.Text;

namespace WebSockets.Core.Test
{
    [TestClass]
    public sealed class TestMessages
    {
        [TestMethod]
        public void TestTextServer()
        {
            AssertMessage(
                new TextMessage("Spam"),
                [129, 4, 83, 112, 97, 109],
                false);
        }

        [TestMethod]
        public void TestTextClient()
        {
            AssertMessage(
                new TextMessage("Spam"),
                [129, 132, 91, 251, 225, 168, 8, 139, 128, 197],
                true);
        }

        [TestMethod]
        public void TestBinaryServer()
        {
            AssertMessage(
                new BinaryMessage(Encoding.UTF8.GetBytes("Eggs")),
                [130, 4, 69, 103, 103, 115],
                false);
        }

        [TestMethod]
        public void TestBinaryClient()
        {
            AssertMessage(
                new BinaryMessage(Encoding.UTF8.GetBytes("Eggs")),
                [130, 132, 83, 205, 226, 137, 22, 170, 133, 250],
                true);
        }

        [TestMethod]
        public void TestNonAsciiTextServer()
        {
            AssertMessage(
                new TextMessage("café"),
                [129, 5, 99, 97, 102, 195, 169],
                false);
        }

        [TestMethod]
        public void TestNonAsciiTextClient()
        {
            AssertMessage(
                new TextMessage("café"),
                [129, 133, 100, 190, 238, 126, 7, 223, 136, 189, 205],
                true);
        }

        [TestMethod]
        public void TestClose()
        {
            AssertMessage(
                new CloseMessage(null, null),
                [136, 0],
                false);
        }

        [TestMethod]
        public void TestPing()
        {
            AssertMessage(
                new PingMessage(Encoding.UTF8.GetBytes("ping")),
                [137, 4, 112, 105, 110, 103],
                false);
        }

        [TestMethod]
        public void TestPong()
        {
            AssertMessage(
                new PongMessage(Encoding.UTF8.GetBytes("pong")),
                [138, 4, 112, 111, 110, 103],
                false);
        }

        public void TestLongPayload()
        {
            var payload = new byte[126];
            Array.Fill(payload, (byte)97);
            byte[] header = [130, 126, 0, 126];
            var expected = new byte[header.Length + payload.Length];
            Array.Copy(header, expected, header.Length);
            Array.Copy(payload, 0, expected, header.Length, expected.Length);
            AssertMessage(
                new BinaryMessage(payload),
                expected,
                false);
        }

        public void TestVeryLongPayload()
        {
            var payload = new byte[65536];
            Array.Fill(payload, (byte)97);
            byte[] header = [130, 127, 0, 0, 0, 0, 0, 1, 0, 0];
            var expected = new byte[header.Length + payload.Length];
            Array.Copy(header, expected, header.Length);
            Array.Copy(payload, 0, expected, header.Length, expected.Length);
            AssertMessage(
                new BinaryMessage(payload),
                expected,
                false);
        }

        private void AssertMessage(Message message, byte[] expected, bool isClient, long maxFrameSize = long.MaxValue)
        {
            var nonceGenerator = isClient ? new MockNonceGenerator(expected.Skip(2).Take(4).ToArray(), "x3JJHMbDL1EzLkh9GBhXDw==") : null;
            var actual = message.Serialize(isClient, Reserved.AllFalse, maxFrameSize, nonceGenerator);
            Assert.IsTrue(actual.SequenceEqual(expected));

            var roundTrip = Message.Deserialize(actual);
            Assert.AreEqual(message, roundTrip);
        }
    }
}

