namespace Conviso.Platform.VisualStudio.Models
{
    public sealed class RequirementDetails
    {
        public string Id { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public string ProjectId { get; set; } = string.Empty;

        public string ProjectLabel { get; set; } = string.Empty;

        public string ChecklistTypeLabel { get; set; } = string.Empty;

        public string ChecksText { get; set; } = string.Empty;
    }
}
