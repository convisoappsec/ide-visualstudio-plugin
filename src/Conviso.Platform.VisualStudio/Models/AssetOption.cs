namespace Conviso.Platform.VisualStudio.Models
{
    public sealed class AssetOption
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string DisplayLabel => string.IsNullOrWhiteSpace(Name) ? Id : $"{Name} ({Id})";
    }
}
