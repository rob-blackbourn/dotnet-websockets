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
            var frame = new Frame(OpCode.Text, true, Reserved.AllFalse, null, Encoding.UTF8.GetBytes("Spam"));
            var actual = frame.Serialize();
            var expected = new byte[] { 129, 4, 83, 112, 97, 109};
            Assert.IsTrue(actual.SequenceEqual(expected));
        }
    }
}

