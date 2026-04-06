using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Conviso.Platform.VisualStudio.ToolWindows;
using Microsoft.VisualStudio.Shell;

namespace Conviso.Platform.VisualStudio.Commands
{
    internal sealed class AnalyzeSecurityAndSuggestFixCommand
    {
        private readonly AsyncPackage package;

        private AnalyzeSecurityAndSuggestFixCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package;
            var menuCommandId = new CommandID(new Guid(PackageGuids.CommandSetString), CommandIds.AnalyzeSecurityAndSuggestFix);
            commandService.AddCommand(new MenuCommand(Execute, menuCommandId));
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            var commandService = await OpenToolWindowCommandBase<ChatToolWindow>.GetCommandServiceAsync(package);
            _ = new AnalyzeSecurityAndSuggestFixCommand(package, commandService);
        }

        private void Execute(object? sender, EventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
            {
                ToolWindowPane window = await package.ShowToolWindowAsync(typeof(ChatToolWindow), 0, true, package.DisposalToken);
                if (window?.Content is ChatToolWindowControl control)
                {
                    await control.RunAnalyzeSecurityAndSuggestFixAsync();
                }
            });
        }
    }
}
