using System;
using System.Collections.Generic;

namespace WebSockets.Core.Messages
{
    /// <summary>
    /// A message with text data.
    /// </summary>
    public class TextMessage : Message, IEquatable<TextMessage>
    {
        public TextMessage(string text)
            : base(MessageType.Text)
        {
            Text = text;
        }
        public string Text { get; private set; }

        public bool Equals(TextMessage? other)
        {
            return base.Equals(other) && Text == other.Text;
        }
    }
}