using System.Windows.Controls;
using Conviso.Platform.VisualStudio.Infrastructure;
using Conviso.Platform.VisualStudio.ViewModels;

namespace Conviso.Platform.VisualStudio.ToolWindows
{
    public partial class RequirementsToolWindowControl : UserControl
    {
        private readonly RequirementsToolWindowViewModel viewModel;

        public RequirementsToolWindowControl(ToolWindowContext context)
        {
            InitializeComponent();
            viewModel = new RequirementsToolWindowViewModel(context.PlatformFacade);
            DataContext = viewModel;
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            try { await viewModel.RefreshAsync(); }
            catch (System.Exception error) { DiagnosticsLogger.LogError("Unable to load requirements: " + error); }
        }
    }
}
