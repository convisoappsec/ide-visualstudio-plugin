using System.Threading.Tasks;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Conviso.Platform.VisualStudio.Infrastructure;
using Conviso.Platform.VisualStudio.Models;
using Conviso.Platform.VisualStudio.ViewModels;

namespace Conviso.Platform.VisualStudio.ToolWindows
{
    public partial class ChatToolWindowControl : UserControl
    {
        private const double MinimumContentHeight = 480;
        private const double VerticalContentMargin = 24;
        private readonly ChatToolWindowViewModel viewModel;
        private readonly DispatcherTimer transcriptScrollTimer;

        public ChatToolWindowControl(ToolWindowContext context)
        {
            InitializeComponent();
            viewModel = new ChatToolWindowViewModel(
                context.SettingsService,
                context.BrokerClient,
                context.EditorContextService,
                context.PatchService);
            DataContext = viewModel;
            transcriptScrollTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = System.TimeSpan.FromMilliseconds(75),
            };
            transcriptScrollTimer.Tick += OnTranscriptScrollTimerTick;
            viewModel.Transcript.CollectionChanged += OnTranscriptChanged;
        }

        private void OnTranscriptChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (ChatTranscriptItem item in e.OldItems)
                {
                    item.PropertyChanged -= OnTranscriptItemPropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (ChatTranscriptItem item in e.NewItems)
                {
                    item.PropertyChanged += OnTranscriptItemPropertyChanged;
                }
            }

            ScheduleTranscriptScroll();

            viewModel.ClearChatCommand.RaiseCanExecuteChanged();
        }

        private void ScrollTranscriptToEnd()
        {
            FindVisualChild<ScrollViewer>(TranscriptList)?.ScrollToEnd();
        }

        private void OnTranscriptItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ChatTranscriptItem.Content))
            {
                ScheduleTranscriptScroll();
            }
        }

        private void ScheduleTranscriptScroll()
        {
            transcriptScrollTimer.Stop();
            transcriptScrollTimer.Start();
        }

        private void OnTranscriptScrollTimerTick(object sender, System.EventArgs e)
        {
            transcriptScrollTimer.Stop();
            ScrollTranscriptToEnd();
        }

        private void OnWindowScrollViewerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Above the minimum, the star-sized transcript follows the viewport.
            // Below it, the content remains fixed and the outer viewer starts scrolling.
            double availableContentHeight = System.Math.Max(0, e.NewSize.Height - VerticalContentMargin);
            LayoutRoot.Height = System.Math.Max(MinimumContentHeight, availableContentHeight);
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
