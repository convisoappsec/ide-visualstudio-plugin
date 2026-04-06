using Microsoft.VisualStudio.Shell;

namespace Conviso.Platform.VisualStudio.ToolWindows
{
    internal sealed class RequirementsToolWindow : ToolWindowPane
    {
        public RequirementsToolWindow() : base(null)
        {
            Caption = "Conviso Requirements";
            Content = new RequirementsToolWindowControl(ConvisoPlatformPackage.Instance!.ToolWindowContext);
        }
    }
}
