using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Conviso.Platform.VisualStudio.Models
{
    public sealed class ChatTranscriptItem : INotifyPropertyChanged
    {
        private string content;

        public ChatTranscriptItem(string role, string content)
        {
            Role = role;
            this.content = content;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Role { get; }

        public string Content
        {
            get => content;
            set
            {
                if (content == value)
                {
                    return;
                }

                content = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Content)));
            }
        }
    }
}
