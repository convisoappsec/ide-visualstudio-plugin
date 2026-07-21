using System.Windows.Controls;
using Conviso.Platform.VisualStudio.Infrastructure;
using Conviso.Platform.VisualStudio.ViewModels;

namespace Conviso.Platform.VisualStudio.ToolWindows
{
    public partial class VulnerabilitiesToolWindowControl : UserControl
    {
        private readonly VulnerabilitiesToolWindowViewModel viewModel;

        public VulnerabilitiesToolWindowControl(ToolWindowContext context)
        {
            InitializeComponent();
            viewModel = new VulnerabilitiesToolWindowViewModel(context.PlatformFacade, context.SettingsService, context.BrokerClient);
            DataContext = viewModel;
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            try { await viewModel.InitializeAsync(); }
            catch (System.Exception error) { DiagnosticsLogger.LogError("Unable to load vulnerabilities: " + error); }
        }
    }
}
