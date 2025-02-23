using System.Linq;
using System.Text;
using WebSockets.Core;

namespace WebSockets.Core.Test
{
    [TestClass]
    public sealed class TestFragmentBuffer
    {
        [TestMethod]
        public void TestConsolidateWrite()
        {
            var fragmentBuffer = new FragmentBuffer();
            fragmentBuffer.Write([0,1,2,3]);
            fragmentBuffer.Write([4,5,6,7]);
            var actual = new byte[8];
            var bytesRead = fragmentBuffer.Read(actual);
            Assert.AreEqual(bytesRead, 8);
            byte[] expected = [0,1,2,3,4,5,6,7];
            Assert.IsTrue(expected.SequenceEqual(actual));
        }
    }
}

