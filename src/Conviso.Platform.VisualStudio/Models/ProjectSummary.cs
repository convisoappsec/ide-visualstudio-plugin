namespace Conviso.Platform.VisualStudio.Models
{
    public sealed class ProjectSummary
    {
        public string Id { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string ProjectTypeLabel { get; set; } = string.Empty;

        public string AssetNames { get; set; } = string.Empty;
    }
}
