using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace Conviso.Platform.VisualStudio.Commands
{
internal sealed class OpenVulnerabilitiesToolWindowCommand : OpenToolWindowCommandBase<ToolWindows.VulnerabilitiesToolWindow>
{
    private OpenVulnerabilitiesToolWindowCommand(AsyncPackage package, OleMenuCommandService commandService)
        : base(package, commandService, CommandIds.OpenVulnerabilitiesToolWindow) { }

    public static async Task InitializeAsync(AsyncPackage package)
    {
        var commandService = await GetCommandServiceAsync(package);
        _ = new OpenVulnerabilitiesToolWindowCommand(package, commandService);
    }
}
}
