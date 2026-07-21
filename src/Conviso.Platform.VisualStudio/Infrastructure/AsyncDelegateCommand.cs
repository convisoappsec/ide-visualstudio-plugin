using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Conviso.Platform.VisualStudio.Infrastructure
{
    internal sealed class AsyncDelegateCommand : ICommand
    {
        private readonly Func<Task> execute;
        private readonly Func<bool>? canExecute;
        private bool isExecuting;

        public AsyncDelegateCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return !isExecuting && (canExecute?.Invoke() ?? true);
        }

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            try
            {
                isExecuting = true;
                RaiseCanExecuteChanged();
                await execute();
            }
            catch (Exception error)
            {
                // ICommand.Execute is async void by contract. Never let an exception
                // escape to WPF's dispatcher, because Visual Studio hosts us in-proc.
                DiagnosticsLogger.LogError("Command failed: " + error);
            }
            finally
            {
                isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
