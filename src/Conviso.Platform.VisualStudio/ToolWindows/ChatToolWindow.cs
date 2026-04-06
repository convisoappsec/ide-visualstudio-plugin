using Microsoft.VisualStudio.Shell;

namespace Conviso.Platform.VisualStudio.ToolWindows
{
    internal sealed class ChatToolWindow : ToolWindowPane
    {
        public ChatToolWindow() : base(null)
        {
            Caption = "Conviso Platform";
            Content = new ChatToolWindowControl(ConvisoPlatformPackage.Instance!.ToolWindowContext);
        }
    }
}
