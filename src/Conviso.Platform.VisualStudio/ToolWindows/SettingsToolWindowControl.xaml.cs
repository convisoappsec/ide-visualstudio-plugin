using System.Windows.Controls;
using Conviso.Platform.VisualStudio.Infrastructure;
using Conviso.Platform.VisualStudio.ViewModels;

namespace Conviso.Platform.VisualStudio.ToolWindows
{
    public partial class SettingsToolWindowControl : UserControl
    {
        public SettingsToolWindowControl(ToolWindowContext context)
        {
            InitializeComponent();
            DataContext = new SettingsToolWindowViewModel(context.SettingsService, context.PlatformFacade);
        }
    }
}
