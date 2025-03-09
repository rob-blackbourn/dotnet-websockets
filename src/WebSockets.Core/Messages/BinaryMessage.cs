using System;
using System.Collections.Generic;

namespace WebSockets.Core.Messages
{
    public class BinaryMessage : DataMessage
    {
        public BinaryMessage(byte[] data)
            : base(MessageType.Binary, data)
        {
        }
    }
}