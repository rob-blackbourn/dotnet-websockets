using System.Linq;
using System.Text;
using WebSockets.Core;

namespace WebSockets.Core.Test
{
    [TestClass]
    public sealed class TestFrames
    {
        [TestMethod]
        public void TestTextUnmasked()
        {
            AssertFrameData(
                OpCode.Text, Encoding.UTF8.GetBytes("Spam"),
                [129, 4, 83, 112, 97, 109],
                false);
        }

        [TestMethod]
        public void TestTextMasked()
        {
            AssertFrameData(
                OpCode.Text, Encoding.UTF8.GetBytes("Spam"),
                [129, 132, 91, 251, 225, 168, 8, 139, 128, 197],
                true
            );
        }

        [TestMethod]
        public void TestBinaryUnmasked()
        {
            AssertFrameData(
                OpCode.Binary, Encoding.UTF8.GetBytes("Eggs"),
                [130, 4, 69, 103, 103, 115],
                false);
        }

        [TestMethod]
        public void TestBinaryMasked()
        {
            AssertFrameData(
                OpCode.Binary, Encoding.UTF8.GetBytes("Eggs"),
                [130, 132, 83, 205, 226, 137, 22, 170, 133, 250],
                true);
        }

        [TestMethod]
        public void TestNonAsciiTextUnmasked()
        {
            AssertFrameData(
                OpCode.Text, Encoding.UTF8.GetBytes("café"),
                [129, 5, 99, 97, 102, 195, 169],
                false);
        }

        [TestMethod]
        public void TestNonAsciiTextMasked()
        {
            AssertFrameData(
                OpCode.Text, Encoding.UTF8.GetBytes("café"),
                [129, 133, 100, 190, 238, 126, 7, 223, 136, 189, 205],
                true);
        }

        [TestMethod]
        public void TestClose()
        {
            AssertFrameData(
                OpCode.Close, [],
                [136, 0],
                false);
        }

        [TestMethod]
        public void TestPing()
        {
            AssertFrameData(
                OpCode.Ping, Encoding.UTF8.GetBytes("ping"),
                [137, 4, 112, 105, 110, 103],
                false);
        }

        [TestMethod]
        public void TestPong()
        {
            AssertFrameData(
                OpCode.Pong, Encoding.UTF8.GetBytes("pong"),
                [138, 4, 112, 111, 110, 103],
                false);
        }

        private void AssertFrameData(OpCode opCode, byte[] payload, byte[] expected, bool isMasked)
        {
            var mask = isMasked ? expected.Skip(2).Take(4).ToArray() : null;
            var frame = new Frame(opCode, true, Reserved.AllFalse, mask, payload);
            var actual = frame.Serialize();
            Assert.IsTrue(actual.SequenceEqual(expected));
        }
    }
}

