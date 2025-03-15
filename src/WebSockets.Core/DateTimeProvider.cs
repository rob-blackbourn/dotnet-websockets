using System;

namespace WebSockets.Core
{
    internal interface IDateTimeProvider
    {
        DateTime Now { get; }
    }

    internal class DateTimeProvider : IDateTimeProvider
    {
        public DateTime Now => DateTime.Now;
    }
}