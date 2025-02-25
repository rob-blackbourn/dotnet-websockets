using System;

namespace WebSockets.Core
{
    public interface INonceGenerator
    {
        byte[] CreateMask();
        string CreateClientKey();
    }

    public class NonceGenerator : INonceGenerator
    {
        public static readonly Random _random;

        static NonceGenerator()
        {
            _random = new Random((int)DateTime.Now.Ticks);
        }

        public byte[] CreateMask()
        {
            var nonce = new byte[4];
            _random.NextBytes(nonce);
            return nonce;
        }

        public string CreateClientKey()
        {
            var nonce = new byte[16];
            _random.NextBytes(nonce);
            return Convert.ToBase64String(nonce);
        }
    }
}