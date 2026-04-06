using System.Windows.Controls;
using Conviso.Platform.VisualStudio.Infrastructure;
using Conviso.Platform.VisualStudio.ViewModels;

namespace Conviso.Platform.VisualStudio.ToolWindows
{
    public partial class PipelineBreaksToolWindowControl : UserControl
    {
        private readonly PipelineBreaksToolWindowViewModel viewModel;

        public PipelineBreaksToolWindowControl(ToolWindowContext context)
        {
            InitializeComponent();
            viewModel = new PipelineBreaksToolWindowViewModel(context.PlatformFacade);
            DataContext = viewModel;
            Loaded += async (_, __) => await viewModel.RefreshAsync();
        }
    }
}
