namespace Conviso.Platform.VisualStudio.Models
{
    public sealed class PipelineBreakSummary
    {
        public string Id { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string ExecutionDate { get; set; } = string.Empty;

        public string TriggeredBy { get; set; } = string.Empty;

        public string AssetName { get; set; } = string.Empty;
    }
}
