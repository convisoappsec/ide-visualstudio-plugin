namespace Conviso.Platform.VisualStudio.Models
{
    public sealed class ProjectDetails
    {
        public string Id { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string CreatedAt { get; set; } = string.Empty;

        public string UpdatedAt { get; set; } = string.Empty;

        public string Goal { get; set; } = string.Empty;

        public string Scope { get; set; } = string.Empty;

        public string StartDate { get; set; } = string.Empty;

        public string EndDate { get; set; } = string.Empty;

        public string EstimatedHours { get; set; } = string.Empty;

        public string ProjectTypeLabel { get; set; } = string.Empty;

        public string TagsText { get; set; } = string.Empty;

        public string AssetsText { get; set; } = string.Empty;

        public string RequirementsText { get; set; } = string.Empty;
    }
}
