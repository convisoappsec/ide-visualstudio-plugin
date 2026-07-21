using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace Conviso.Platform.VisualStudio.Commands
{
internal abstract class OpenToolWindowCommandBase<TToolWindow> where TToolWindow : ToolWindowPane
{
    private readonly AsyncPackage package;

    protected OpenToolWindowCommandBase(AsyncPackage package, OleMenuCommandService commandService, int commandId)
    {
        this.package = package;
        var menuCommandId = new CommandID(new Guid(PackageGuids.CommandSetString), commandId);
        commandService.AddCommand(new MenuCommand(Execute, menuCommandId));
    }

    internal static async Task<OleMenuCommandService> GetCommandServiceAsync(AsyncPackage package)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
        if (commandService == null)
        {
            throw new InvalidOperationException("OleMenuCommandService is not available.");
        }

        return commandService;
    }

    private void Execute(object? sender, EventArgs e)
    {
        ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
        {
            try
            {
                ToolWindowPane window = await package.ShowToolWindowAsync(typeof(TToolWindow), 0, true, package.DisposalToken);
                if (window == null)
                {
                    throw new InvalidOperationException($"Unable to open tool window {typeof(TToolWindow).Name}.");
                }
            }
            catch (OperationCanceledException) when (package.DisposalToken.IsCancellationRequested)
            {
                // Visual Studio is shutting down.
            }
            catch (Exception error)
            {
                Infrastructure.DiagnosticsLogger.LogError($"Unable to open {typeof(TToolWindow).Name}: {error}");
            }
        });
    }
}
}
