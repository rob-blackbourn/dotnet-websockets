using System;

namespace WebSockets.Core
{
    public interface INonceGenerator
    {
        byte[] Create();
    }

    public class NonceGenerator : INonceGenerator
    {
        public static readonly Random _random;

        static NonceGenerator()
        {
            _random = new Random((int)DateTime.Now.Ticks);
        }

        public byte[] Create()
        {
            var buf = new byte[4];
            _random.NextBytes(buf);
            return buf;
        }
    }
}