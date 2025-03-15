using System;

namespace WebSockets.Core.Test
{
    class MockDateTimeProvider : IDateTimeProvider
    {
        public MockDateTimeProvider(DateTime now)
        {
            Now = now;
        }

        public DateTime Now { get; }
    }
}