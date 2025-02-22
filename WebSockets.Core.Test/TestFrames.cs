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

        private void AssertFrameData(OpCode opCode, byte[] payload, byte[] expected, bool isMasked)
        {
            var mask = isMasked ? expected.Skip(2).Take(4).ToArray() : null;
            var frame = new Frame(OpCode.Text, true, Reserved.AllFalse, mask, payload);
            var actual = frame.Serialize();
            Assert.IsTrue(actual.SequenceEqual(expected));
        }
    }
}

