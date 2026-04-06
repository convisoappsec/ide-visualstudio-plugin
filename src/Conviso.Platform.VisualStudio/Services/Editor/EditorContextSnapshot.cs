namespace Conviso.Platform.VisualStudio.Services.Editor
{
    public sealed class EditorContextSnapshot
    {
        public string FilePath { get; set; } = string.Empty;

        public string Language { get; set; } = "text";

        public string SelectionText { get; set; } = string.Empty;

        public string DocumentText { get; set; } = string.Empty;

        public bool HasSelection => !string.IsNullOrWhiteSpace(SelectionText);
    }
}
