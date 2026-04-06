namespace Conviso.Platform.VisualStudio.Models
{
    public sealed class ProjectActivitySummary
    {
        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string PermittedStatusText { get; set; } = string.Empty;

        public string AssigneeEmailsText { get; set; } = string.Empty;

        public string CheckLabel { get; set; } = string.Empty;

        public string CheckDescription { get; set; } = string.Empty;

        public string UpdatedAt { get; set; } = string.Empty;
    }
}
