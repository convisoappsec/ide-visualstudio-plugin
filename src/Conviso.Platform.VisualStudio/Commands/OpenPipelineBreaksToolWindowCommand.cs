using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace Conviso.Platform.VisualStudio.Commands
{
internal sealed class OpenPipelineBreaksToolWindowCommand : OpenToolWindowCommandBase<ToolWindows.PipelineBreaksToolWindow>
{
    private OpenPipelineBreaksToolWindowCommand(AsyncPackage package, OleMenuCommandService commandService)
        : base(package, commandService, CommandIds.OpenPipelineBreaksToolWindow) { }

    public static async Task InitializeAsync(AsyncPackage package)
    {
        var commandService = await GetCommandServiceAsync(package);
        _ = new OpenPipelineBreaksToolWindowCommand(package, commandService);
    }
}
}
