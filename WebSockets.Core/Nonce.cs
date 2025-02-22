using System;

namespace WebSockets.Core
{
    public interface INonceGenerator
    {
        void GetBytes(byte[] buffer);
        byte[] Create();
    }

    public class NonceGenerator : INonceGenerator
    {
        public static readonly Random _random;

        static NonceGenerator()
        {
            _random = new Random((int)DateTime.Now.Ticks);
        }

        public void GetBytes(byte[] buffer)
        {
            _random.NextBytes(buffer);
        }

        public byte[] Create()
        {
            var buf = new byte[4];
            GetBytes(buf);
            return buf;
        }
    }
}