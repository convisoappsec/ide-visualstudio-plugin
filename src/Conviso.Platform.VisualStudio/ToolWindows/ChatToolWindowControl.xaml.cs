using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
                ScrollTranscriptToEnd();
            }

            viewModel.ClearChatCommand.RaiseCanExecuteChanged();
        }

        private void ScrollTranscriptToEnd()
        {
            TranscriptList.UpdateLayout();
            FindVisualChild<ScrollViewer>(TranscriptList)?.ScrollToEnd();
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int index = 0; index < childrenCount; index++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, index);
                if (child is T match)
                {
                    return match;
                }

                T? nestedMatch = FindVisualChild<T>(child);
                if (nestedMatch != null)
                {
                    return nestedMatch;
                }
            }

            return null;
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
