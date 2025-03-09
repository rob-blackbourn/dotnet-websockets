using System;
using System.Collections.Generic;

namespace WebSockets.Core.Messages
{
    public class PongMessage : DataMessage
    {
        public PongMessage(byte[] data)
            : base(MessageType.Pong, data)
        {
        }
    }
}