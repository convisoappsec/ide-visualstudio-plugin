namespace Conviso.Platform.VisualStudio.Models
{
    public sealed class AccessibleCompanyOption
    {
        public string Id { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public bool Active { get; set; }

        public bool Configured { get; set; }

        public string DisplayLabel => string.IsNullOrWhiteSpace(Label) ? Id : $"{Label} ({Id})";
    }
}
