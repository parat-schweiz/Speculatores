using System;

namespace Speculatores
{
    public class Message
    {
        public Guid Id { get; private set; }

        public string Text { get; private set; }

        public Message(Guid id, string text)
        {
            Text = text;
        }
    }
}
