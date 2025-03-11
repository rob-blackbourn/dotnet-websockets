using System;

namespace WebSockets.Core.Messages
{
    /// <summary>
    /// A message with text data.
    /// </summary>
    public class TextMessage : Message, IEquatable<TextMessage>
    {
        /// <summary>
        /// Construct the text message.
        /// </summary>
        /// <param name="text">The message text.</param>
        public TextMessage(string text)
            : base(MessageType.Text)
        {
            Text = text;
        }

        /// <summary>
        /// The message text.
        /// </summary>
        /// <value>The text.</value>
        public string Text { get; private set; }

        /// <inheritdoc />
        public bool Equals(TextMessage? other)
        {
            return base.Equals(other) && Text == other.Text;
        }
    }
}