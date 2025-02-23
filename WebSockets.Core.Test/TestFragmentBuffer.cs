using System.Linq;
using System.Text;
using WebSockets.Core;

namespace WebSockets.Core.Test
{
    [TestClass]
    public sealed class TestFragmentBuffer
    {
        [TestMethod]
        public void TestWriteManyReadOnce()
        {
            var fragmentBuffer = new FragmentBuffer<byte>();
            fragmentBuffer.Write([0, 1, 2, 3]);
            fragmentBuffer.Write([4, 5, 6, 7]);
            var actual = new byte[8];
            var bytesRead = fragmentBuffer.Read(actual);
            Assert.AreEqual(bytesRead, 8);
            byte[] expected = [0, 1, 2, 3, 4, 5, 6, 7];
            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestMethod]
        public void TestWriteOnceReadMany()
        {
            var fragmentBuffer = new FragmentBuffer<byte>();
            fragmentBuffer.Write([0, 1, 2, 3, 4, 5, 6, 7]);

            var actual = new byte[4];

            var bytesRead = fragmentBuffer.Read(actual);
            Assert.AreEqual(bytesRead, 4);
            byte[] expected = [0, 1, 2, 3];
            Assert.IsTrue(expected.SequenceEqual(actual));

            bytesRead = fragmentBuffer.Read(actual);
            Assert.AreEqual(bytesRead, 4);
            expected = [4, 5, 6, 7];
            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestMethod]
        public void TestReadMoreThanWritten()
        {
            var fragmentBuffer = new FragmentBuffer<byte>();
            fragmentBuffer.Write([0, 1, 2, 3]);

            byte[] actual = [0, 0, 0, 0, 0, 0, 0, 0];
            var bytesRead = fragmentBuffer.Read(actual);
            Assert.AreEqual(bytesRead, 4);
            byte[] expected = [0, 1, 2, 3, 0, 0, 0, 0];
            Assert.IsTrue(expected.SequenceEqual(actual));
        }
    }
}

