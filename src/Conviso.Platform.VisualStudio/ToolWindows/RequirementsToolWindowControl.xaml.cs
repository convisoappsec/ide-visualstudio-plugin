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
            Loaded += async (_, __) => await viewModel.RefreshAsync();
        }
    }
}
