namespace WebSockets.Core.Test
{
    class MockNonceGenerator : INonceGenerator
    {
        public MockNonceGenerator(byte[] mask)
        {
            Mask = mask;
        }

        public byte[] Mask { get; private set; }

        public byte[] Create()
        {
            return Mask;
        }
    }
}