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

        private void AssertFrameData(OpCode opCode, byte[] payload, byte[] expected, bool isMasked)
        {
            var mask = isMasked ? expected.Skip(2).Take(4).ToArray() : null;
            var frame = new Frame(opCode, true, Reserved.AllFalse, mask, payload);
            var actual = frame.Serialize();
            Assert.IsTrue(actual.SequenceEqual(expected));
        }
    }
}

