namespace Conviso.Platform.VisualStudio.Models
{
    public sealed class ChatMessage
    {
        public ChatMessage(string role, string content, string language = "text")
        {
            Role = role;
            Content = content;
            Language = language;
        }

        public string Role { get; }

        public string Content { get; }

        public string Language { get; }
    }
}
