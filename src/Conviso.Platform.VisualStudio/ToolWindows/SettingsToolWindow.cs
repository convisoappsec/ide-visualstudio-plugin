using Microsoft.VisualStudio.Shell;

namespace Conviso.Platform.VisualStudio.ToolWindows
{
    internal sealed class SettingsToolWindow : ToolWindowPane
    {
        public SettingsToolWindow() : base(null)
        {
            Caption = "Conviso Settings";
            Content = new SettingsToolWindowControl(ConvisoPlatformPackage.Instance!.ToolWindowContext);
        }
    }
}
