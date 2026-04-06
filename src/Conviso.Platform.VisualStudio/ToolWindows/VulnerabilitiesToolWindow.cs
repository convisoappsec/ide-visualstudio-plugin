using Microsoft.VisualStudio.Shell;

namespace Conviso.Platform.VisualStudio.ToolWindows
{
    internal sealed class VulnerabilitiesToolWindow : ToolWindowPane
    {
        public VulnerabilitiesToolWindow() : base(null)
        {
            Caption = "Conviso Vulnerabilities";
            Content = new VulnerabilitiesToolWindowControl(ConvisoPlatformPackage.Instance!.ToolWindowContext);
        }
    }
}
