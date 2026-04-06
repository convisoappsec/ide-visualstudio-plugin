using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace Conviso.Platform.VisualStudio.Commands
{
internal sealed class OpenRequirementsToolWindowCommand : OpenToolWindowCommandBase<ToolWindows.RequirementsToolWindow>
{
    private OpenRequirementsToolWindowCommand(AsyncPackage package, OleMenuCommandService commandService)
        : base(package, commandService, CommandIds.OpenRequirementsToolWindow) { }

    public static async Task InitializeAsync(AsyncPackage package)
    {
        var commandService = await GetCommandServiceAsync(package);
        _ = new OpenRequirementsToolWindowCommand(package, commandService);
    }
}
}
