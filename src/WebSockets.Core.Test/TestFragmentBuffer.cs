using System.Linq;

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
        public void TestWriteReadWriteRead()
        {
            var actual = new byte[4];

            var fragmentBuffer = new FragmentBuffer<byte>();
            fragmentBuffer.Write([0, 1, 2, 3], 0, 4);
            var bytesRead = fragmentBuffer.Read(actual);
            Assert.AreEqual(bytesRead, 4);
            byte[] expected = [0, 1, 2, 3];
            Assert.IsTrue(expected.SequenceEqual(actual));

            fragmentBuffer.Write([4, 5, 6, 7], 0, 4);
            bytesRead = fragmentBuffer.Read(actual);
            Assert.AreEqual(bytesRead, 4);
            expected = [4, 5, 6, 7];
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

        [TestMethod]
        public void TestReadWhenEmpty()
        {
            var fragmentBuffer = new FragmentBuffer<byte>();
            var actual = new byte[8];
            var bytesRead = fragmentBuffer.Read(actual);
            Assert.AreEqual(bytesRead, 0);
        }

        [TestMethod]
        public void TestIndex()
        {
            var fragmentBuffer = new FragmentBuffer<long>();
            fragmentBuffer.Write([0, 1, 2, 3, 4]);
            fragmentBuffer.Write([5, 6, 7, 8, 9]);
            for (var i = 0L; i < fragmentBuffer.Count; ++i)
            {
                Assert.AreEqual(i, fragmentBuffer[i]);
            }
        }

        [TestMethod]
        public void TestIndexOf()
        {
            var fragmentBuffer = new FragmentBuffer<int>();
            fragmentBuffer.Write([0, 1, 2, 3, 4]);
            fragmentBuffer.Write([5, 6, 7, 8, 9]);
            fragmentBuffer.Write([0, 1, 2, 3, 4]);
            var a = fragmentBuffer.IndexOf([0, 1, 2, 3], 0);
            Assert.AreEqual(0, a);
            var b = fragmentBuffer.IndexOf([0, 1, 2, 3], 1);
            Assert.AreEqual(10, b);
        }

        [TestMethod]
        public void TestEndsWith()
        {
            var fragmentBuffer = new FragmentBuffer<int>();
            fragmentBuffer.Write([0, 1, 2]);
            fragmentBuffer.Write([3, 4, 5]);
            fragmentBuffer.Write([6, 7, 8]);
            Assert.IsTrue(fragmentBuffer.EndsWith([8]));
            Assert.IsTrue(fragmentBuffer.EndsWith([7, 8]));
            Assert.IsTrue(fragmentBuffer.EndsWith([6, 7, 8]));
            Assert.IsTrue(fragmentBuffer.EndsWith([5, 6, 7, 8]));
            Assert.IsFalse(fragmentBuffer.EndsWith([5, 6, 7]));
        }

        [TestMethod]
        public void TestToArray()
        {
            var fragmentBuffer = new FragmentBuffer<int>();
            fragmentBuffer.Write([0, 1, 2]);
            fragmentBuffer.Write([3, 4, 5]);
            fragmentBuffer.Write([6, 7, 8]);
            var actual = fragmentBuffer.ToArray();
            int[] expected = [0, 1, 2, 3, 4, 5, 6, 7, 8];
            Assert.IsTrue(expected.SequenceEqual(actual));
        }
    }
}

