using System.Collections.Generic;

namespace Conviso.Platform.VisualStudio.Services.Editor
{
    public sealed class WorkspaceContextSnapshot
    {
        public string RootPath { get; set; } = string.Empty;

        public List<WorkspaceFileContext> Files { get; } = new List<WorkspaceFileContext>();
    }

    public sealed class WorkspaceFileContext
    {
        public string FilePath { get; set; } = string.Empty;

        public string Language { get; set; } = "text";

        public string Content { get; set; } = string.Empty;
    }
}
