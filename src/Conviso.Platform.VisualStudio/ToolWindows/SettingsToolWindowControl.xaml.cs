using System.Windows.Controls;
using Conviso.Platform.VisualStudio.Infrastructure;
using Conviso.Platform.VisualStudio.ViewModels;

namespace Conviso.Platform.VisualStudio.ToolWindows
{
    public partial class SettingsToolWindowControl : UserControl
    {
        private readonly SettingsToolWindowViewModel viewModel;

        public SettingsToolWindowControl(ToolWindowContext context)
        {
            InitializeComponent();
            viewModel = new SettingsToolWindowViewModel(context.SettingsService, context.PlatformFacade);
            DataContext = viewModel;
            ApiTokenPasswordBox.Password = viewModel.ApiToken;
            SettingsTabs.SelectionChanged += OnSettingsTabSelectionChanged;
        }

        private void OnApiTokenPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is SettingsToolWindowViewModel currentViewModel)
            {
                currentViewModel.ApiToken = ApiTokenPasswordBox.Password;
            }
        }

        private async void OnSettingsTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source == SettingsTabs && ScopeTab.IsSelected)
            {
                await viewModel.LoadCompaniesAsync();
            }
        }
    }
}
