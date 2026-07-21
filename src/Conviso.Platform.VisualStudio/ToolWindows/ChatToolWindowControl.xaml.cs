using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Windows.Controls;
using Conviso.Platform.VisualStudio.Infrastructure;
using Conviso.Platform.VisualStudio.ViewModels;

namespace Conviso.Platform.VisualStudio.ToolWindows
{
    public partial class ChatToolWindowControl : UserControl
    {
        private readonly ChatToolWindowViewModel viewModel;

        public ChatToolWindowControl(ToolWindowContext context)
        {
            InitializeComponent();
            viewModel = new ChatToolWindowViewModel(
                context.SettingsService,
                context.BrokerClient,
                context.EditorContextService,
                context.PatchService);
            DataContext = viewModel;
            viewModel.Transcript.CollectionChanged += OnTranscriptChanged;
        }

        private void OnTranscriptChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (viewModel.Transcript.Count > 0)
            {
                TranscriptList.ScrollIntoView(viewModel.Transcript[viewModel.Transcript.Count - 1]);
            }

            viewModel.ClearChatCommand.RaiseCanExecuteChanged();
        }

        public Task RunAnalyzeSecurityAndSuggestFixAsync()
        {
            return viewModel.AnalyzeSecurityAndSuggestFixAsync();
        }

        public Task RunAttachSelectionAsync()
        {
            return viewModel.AttachSelectionAsync();
        }

        public Task RunCheckSimilarIssuesAsync()
        {
            return viewModel.CheckSimilarIssuesAsync();
        }

        public Task RunApplySuggestedFixAsync()
        {
            return viewModel.ApplySuggestedFixAsync();
        }
    }
}
