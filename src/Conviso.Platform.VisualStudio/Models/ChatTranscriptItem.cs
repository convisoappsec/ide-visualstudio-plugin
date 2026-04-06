namespace Conviso.Platform.VisualStudio.Models
{
    public sealed class ChatTranscriptItem
    {
        public ChatTranscriptItem(string role, string content)
        {
            Role = role;
            Content = content;
        }

        public string Role { get; }

        public string Content { get; }
    }
}
