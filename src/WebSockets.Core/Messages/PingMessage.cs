using System;
using System.Collections.Generic;

namespace WebSockets.Core.Messages
{
    public class PingMessage : DataMessage
    {
        public PingMessage(byte[] data)
            : base(MessageType.Ping, data)
        {
        }
    }
}