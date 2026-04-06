using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace Conviso.Platform.VisualStudio.Commands
{
internal sealed class OpenChatToolWindowCommand : OpenToolWindowCommandBase<ToolWindows.ChatToolWindow>
{
    private OpenChatToolWindowCommand(AsyncPackage package, OleMenuCommandService commandService)
        : base(package, commandService, CommandIds.OpenChatToolWindow) { }

    public static async Task InitializeAsync(AsyncPackage package)
    {
        var commandService = await GetCommandServiceAsync(package);
        _ = new OpenChatToolWindowCommand(package, commandService);
    }
}
}
