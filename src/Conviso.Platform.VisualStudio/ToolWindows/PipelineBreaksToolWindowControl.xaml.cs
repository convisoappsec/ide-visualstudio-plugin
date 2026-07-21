using System.Windows.Controls;
using Conviso.Platform.VisualStudio.Infrastructure;
using Conviso.Platform.VisualStudio.ViewModels;

namespace Conviso.Platform.VisualStudio.ToolWindows
{
    public partial class PipelineBreaksToolWindowControl : UserControl
    {
        private const double MinimumContentHeight = 520;
        private const double VerticalContentMargin = 24;
        private readonly PipelineBreaksToolWindowViewModel viewModel;

        public PipelineBreaksToolWindowControl(ToolWindowContext context)
        {
            InitializeComponent();
            viewModel = new PipelineBreaksToolWindowViewModel(context.PlatformFacade);
            DataContext = viewModel;
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            try { await viewModel.RefreshAsync(); }
            catch (System.Exception error) { DiagnosticsLogger.LogError("Unable to load pipeline breaks: " + error); }
        }

        private void OnWindowScrollViewerSizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            double availableContentHeight = System.Math.Max(0, e.NewSize.Height - VerticalContentMargin);
            LayoutRoot.Height = System.Math.Max(MinimumContentHeight, availableContentHeight);
        }
    }
}
