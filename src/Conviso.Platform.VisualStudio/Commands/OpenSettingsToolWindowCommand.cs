using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace Conviso.Platform.VisualStudio.Commands
{
    internal sealed class OpenSettingsToolWindowCommand : OpenToolWindowCommandBase<ToolWindows.SettingsToolWindow>
    {
        private OpenSettingsToolWindowCommand(AsyncPackage package, OleMenuCommandService commandService)
            : base(package, commandService, CommandIds.OpenSettingsToolWindow) { }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            var commandService = await GetCommandServiceAsync(package);
            _ = new OpenSettingsToolWindowCommand(package, commandService);
        }
    }
}
