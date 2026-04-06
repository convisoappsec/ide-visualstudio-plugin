namespace Conviso.Platform.VisualStudio.Models
{
    public sealed class PipelineBreakDetails
    {
        public string Id { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string ExecutionDate { get; set; } = string.Empty;

        public string TriggeredBy { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;

        public string AssetName { get; set; } = string.Empty;

        public string ReasonText { get; set; } = string.Empty;
    }
}
