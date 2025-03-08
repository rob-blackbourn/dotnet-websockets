using System.Linq;

namespace WebSockets.Core.Test
{
    [TestClass]
    public sealed class TestArrayBuffer
    {
        [TestMethod]
        public void TestToArray()
        {
            var a = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var b = new ArrayBuffer<int>(a);
            Assert.IsTrue(b.ToArray().SequenceEqual(a));

            var c = b.Slice(0, 3);
            Assert.IsTrue(c.ToArray().SequenceEqual([0, 1, 2]));

            var d = b.Slice(3, 4);
            Assert.IsTrue(d.ToArray().SequenceEqual([3, 4, 5, 6]));
        }

        [TestMethod]
        public void TestCopyInto()
        {
            var a = new ArrayBuffer<int>([3, 4, 5, 6]);
            int[] b = [0, 1, 2, 9, 9, 9, 9];
            a.CopyInto(b, 3, b.LongLength);
            Assert.IsTrue(b.SequenceEqual([0, 1, 2, 3, 4, 5, 6]));
            Assert.AreEqual(a.Count, 0);
        }
    }
}

