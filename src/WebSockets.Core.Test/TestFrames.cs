using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public void TestLongPayload()
        {
            var payload = new byte[126];
            Array.Fill(payload, (byte)97);
            byte[] header = [130, 126, 0, 126];
            var expected = new byte[header.Length + payload.Length];
            Array.Copy(header, expected, header.Length);
            Array.Copy(payload, 0, expected, header.Length, expected.Length);
            AssertFrameData(
                OpCode.Binary, payload,
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
            AssertFrameData(
                OpCode.Binary, payload,
                expected,
                false);
        }

        private void AssertFrameData(OpCode opCode, byte[] payload, byte[] expected, bool isMasked)
        {
            var mask = isMasked ? expected.Skip(2).Take(4).ToArray() : null;
            var frame = new Frame(opCode, true, Reserved.AllFalse, mask, payload);

            // Serialize.
            var writer = new FrameWriter();
            writer.WriteFrame(frame);
            var buffers = new List<ArrayBuffer<byte>>();
            while (writer.HasData)
            {
                var buf = new byte[1024];
                var offset = 0L;
                writer.ReadData(buf, ref offset, buf.LongLength);
                buffers.Add(new ArrayBuffer<byte>(buf, 0, offset));
            }
            var actual = buffers.ToFlatArray();

            Assert.IsTrue(actual.SequenceEqual(expected));

            // Deserialize.
            var reader = new FrameReader();
            reader.WriteData(actual, 0, actual.Length);
            if (!reader.HasFrame)
                throw new InvalidOperationException("failed to deserialize");
            var roundTrip = reader.ReadFrame();

            Assert.AreEqual(frame, roundTrip);
        }
    }
}

