using Microsoft.VisualStudio.Shell;

namespace Conviso.Platform.VisualStudio.ToolWindows
{
    internal sealed class ChatToolWindow : ToolWindowPane
    {
        public ChatToolWindow() : base(null)
        {
            Caption = "AI Autonomous AppSec";
            Content = new ChatToolWindowControl(ConvisoPlatformPackage.Instance!.ToolWindowContext);
        }
    }
}
