using Microsoft.VisualStudio.Shell;

namespace Conviso.Platform.VisualStudio.ToolWindows
{
    internal sealed class PipelineBreaksToolWindow : ToolWindowPane
    {
        public PipelineBreaksToolWindow() : base(null)
        {
            Caption = "Conviso Pipeline Breaks";
            Content = new PipelineBreaksToolWindowControl(ConvisoPlatformPackage.Instance!.ToolWindowContext);
        }
    }
}
