using System;
using System.ComponentModel.Design;
using System.Threading;
using System.Threading.Tasks;
using Conviso.Platform.VisualStudio.ToolWindows;
using Microsoft.VisualStudio.Shell;

namespace Conviso.Platform.VisualStudio.Commands
{
    internal sealed class AttachSelectionToChatCommand
    {
        private readonly AsyncPackage package;
        private int isExecuting;

        private AttachSelectionToChatCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package;
            var menuCommandId = new CommandID(new Guid(PackageGuids.CommandSetString), CommandIds.AttachSelectionToChat);
            commandService.AddCommand(new MenuCommand(Execute, menuCommandId));
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            var commandService = await OpenToolWindowCommandBase<ChatToolWindow>.GetCommandServiceAsync(package);
            _ = new AttachSelectionToChatCommand(package, commandService);
        }

        private void Execute(object? sender, EventArgs e)
        {
            if (Interlocked.Exchange(ref isExecuting, 1) != 0)
            {
                return;
            }

            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
            {
                try
                {
                    ToolWindowPane window = await package.ShowToolWindowAsync(typeof(ChatToolWindow), 0, true, package.DisposalToken);
                    if (window?.Content is ChatToolWindowControl control)
                    {
                        await control.RunAttachSelectionAsync();
                    }
                }
                catch (OperationCanceledException) when (package.DisposalToken.IsCancellationRequested)
                {
                   
                }
                catch (Exception error)
                {
                    Infrastructure.DiagnosticsLogger.LogError("Attach Selection failed: " + error);
                }
                finally
                {
                    Volatile.Write(ref isExecuting, 0);
                }
            });
        }
    }
}
