namespace WebSockets.Core.Test
{
    class MockNonceGenerator : INonceGenerator
    {
        public MockNonceGenerator(byte[] mask, string clientKey)
        {
            Mask = mask;
            ClientKey = clientKey;
        }

        public byte[] Mask { get; private set; }
        public string ClientKey { get; private set; }

        public byte[] CreateMask()
        {
            return Mask;
        }

        public string CreateClientKey()
        {
            return ClientKey;
        }
    }
}