using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Conviso.Platform.VisualStudio.Configuration;
using Conviso.Platform.VisualStudio.Infrastructure;
using Conviso.Platform.VisualStudio.Models;
using Conviso.Platform.VisualStudio.Services.Broker;
using Conviso.Platform.VisualStudio.Services.Editor;
using Conviso.Platform.VisualStudio.Services.Patching;
using Microsoft.VisualStudio.Shell;

namespace Conviso.Platform.VisualStudio.ViewModels
{
internal sealed class ChatToolWindowViewModel : ObservableObject
{
    private readonly ISettingsService settingsService;
    private readonly IBrokerClient brokerClient;
    private readonly IEditorContextService editorContextService;
    private readonly IPatchService patchService;
    private readonly SemaphoreSlim connectionLock = new SemaphoreSlim(1, 1);
    private readonly Dictionary<string, int> requestExtractorIdMap = new Dictionary<string, int>();
    private EditorContextSnapshot? attachedContext;
    private string? latestCompletedRequestId;
    private string status = "Ready";
    private string message = string.Empty;
    private string attachedContextSummary = "No attached selection.";
    private string thinkingStatus = string.Empty;

    public ChatToolWindowViewModel(
        ISettingsService settingsService,
        IBrokerClient brokerClient,
        IEditorContextService editorContextService,
        IPatchService patchService)
    {
        this.settingsService = settingsService;
        this.brokerClient = brokerClient;
        this.editorContextService = editorContextService;
        this.patchService = patchService;
        Transcript = new ObservableCollection<ChatTranscriptItem>();
        SendCommand = new AsyncDelegateCommand(SendAsync, () => !string.IsNullOrWhiteSpace(Message));
        AttachSelectionCommand = new AsyncDelegateCommand(AttachSelectionAsync);
        AnalyzeSelectionCommand = new AsyncDelegateCommand(AnalyzeSecurityAndSuggestFixAsync);
        CheckSimilarIssuesCommand = new AsyncDelegateCommand(CheckSimilarIssuesAsync);
        ApplySuggestedFixCommand = new AsyncDelegateCommand(ApplySuggestedFixAsync, CanApplySuggestedFix);
        MarkResponseHelpfulCommand = new AsyncDelegateCommand(MarkResponseHelpfulAsync, CanMarkResponseHelpful);
        ClearAttachedContextCommand = new AsyncDelegateCommand(ClearAttachedContextAsync, () => attachedContext != null);
        ClearChatCommand = new AsyncDelegateCommand(ClearChatAsync, () => Transcript.Count > 0);
        brokerClient.EventReceived += OnBrokerEventReceived;
    }

    public string Status
    {
        get => status;
        set => SetProperty(ref status, value);
    }

    public string Message
    {
        get => message;
        set
        {
            if (SetProperty(ref message, value))
            {
                SendCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string ThinkingStatus
    {
        get => thinkingStatus;
        set => SetProperty(ref thinkingStatus, value);
    }

    public AsyncDelegateCommand SendCommand { get; }

    public AsyncDelegateCommand AttachSelectionCommand { get; }

    public AsyncDelegateCommand AnalyzeSelectionCommand { get; }

    public AsyncDelegateCommand CheckSimilarIssuesCommand { get; }

    public AsyncDelegateCommand ApplySuggestedFixCommand { get; }

    public AsyncDelegateCommand MarkResponseHelpfulCommand { get; }

    public AsyncDelegateCommand ClearAttachedContextCommand { get; }

    public AsyncDelegateCommand ClearChatCommand { get; }

    public ObservableCollection<ChatTranscriptItem> Transcript { get; }

    public string AttachedContextSummary
    {
        get => attachedContextSummary;
        set => SetProperty(ref attachedContextSummary, value);
    }

    private async Task SendAsync()
    {
        string currentMessage = Message.Trim();
        if (string.IsNullOrWhiteSpace(currentMessage))
        {
            return;
        }

        try
        {
            ThinkingStatus = "Thinking...";
            await EnsureConnectedAsync();
            Transcript.Add(new ChatTranscriptItem("user", currentMessage));
            var context = attachedContext ?? await editorContextService.GetActiveContextAsync(CancellationToken.None);
            string brokerMessage = BuildBrokerMessage(currentMessage, context);
            string language = context?.Language ?? "text";
            await brokerClient.SendChatMessageAsync(new ChatMessage("user", brokerMessage, language), CancellationToken.None);
            Status = "Message sent";
            Message = string.Empty;
        }
        catch (System.Exception error)
        {
            ThinkingStatus = string.Empty;
            Status = "Send failed";
            Transcript.Add(new ChatTranscriptItem("system", "Failed to send: " + error.Message));
        }
    }

    public async Task AttachSelectionAsync()
    {
        try
        {
            var context = await editorContextService.GetActiveContextAsync(CancellationToken.None);
            if (context == null || !context.HasSelection)
            {
                Status = "Selection required";
                Transcript.Add(new ChatTranscriptItem("system", "Select a code snippet first, then attach it to the chat."));
                return;
            }

            attachedContext = context;
            AttachedContextSummary = BuildAttachedContextSummary(context);
            ClearAttachedContextCommand.RaiseCanExecuteChanged();
            Status = "Selection attached";
            Transcript.Add(new ChatTranscriptItem("system", "Selected code attached to the chat context."));
        }
        catch (System.Exception error)
        {
            Status = "Attach failed";
            Transcript.Add(new ChatTranscriptItem("system", "Failed to attach selection: " + error.Message));
        }
    }

    public async Task AnalyzeSecurityAndSuggestFixAsync()
    {
        try
        {
            var context = await editorContextService.GetActiveContextAsync(CancellationToken.None);
            if (context == null || !context.HasSelection)
            {
                Status = "Selection required";
                Transcript.Add(new ChatTranscriptItem("system", "Select a code snippet first, then run Analyze + Suggest Fix."));
                return;
            }

            string userMessage = "Analyze the selected code and suggest a fix.";
            string request = context.SelectionText;

            ThinkingStatus = "Thinking...";
            await EnsureConnectedAsync();
            Transcript.Add(new ChatTranscriptItem("user", userMessage));
            await brokerClient.SendChatMessageAsync(new ChatMessage("user", request, context.Language), CancellationToken.None);
            Status = "Analysis requested";
        }
        catch (System.Exception error)
        {
            ThinkingStatus = string.Empty;
            Status = "Analyze failed";
            Transcript.Add(new ChatTranscriptItem("system", "Failed to analyze selection: " + error.Message));
        }
    }

    public async Task CheckSimilarIssuesAsync()
    {
        try
        {
            var context = attachedContext ?? await editorContextService.GetActiveContextAsync(CancellationToken.None);
            if (context == null || !context.HasSelection)
            {
                Status = "Selection required";
                Transcript.Add(new ChatTranscriptItem("system", "Select or attach a code snippet first so the extension can look for similar issues."));
                return;
            }

            var workspaceContext = await editorContextService.GetWorkspaceContextAsync(context, CancellationToken.None);
            int workspaceFilesCount = workspaceContext?.Files.Count ?? 0;
            if (workspaceFilesCount == 0)
            {
                Status = "Workspace scan unavailable";
                Transcript.Add(new ChatTranscriptItem("system", "No workspace files were collected for the similarity scan."));
                return;
            }

            string request = BuildSimilarIssuesMessage(context, workspaceContext!);
            string userMessage = "Check whether the selected issue appears elsewhere in the workspace (" + workspaceFilesCount + " files scanned).";

            ThinkingStatus = "Thinking...";
            await EnsureConnectedAsync();
            Transcript.Add(new ChatTranscriptItem("user", userMessage));
            await brokerClient.SendChatMessageAsync(new ChatMessage("user", request, context.Language), CancellationToken.None);
            Status = "Similarity scan requested";
        }
        catch (System.Exception error)
        {
            ThinkingStatus = string.Empty;
            Status = "Similarity scan failed";
            Transcript.Add(new ChatTranscriptItem("system", "Failed to check similar issues: " + error.Message));
        }
    }

    public async Task ApplySuggestedFixAsync()
    {
        try
        {
            if (!TryGetLatestSuggestedFix(out string replacement))
            {
                Status = "No suggested fix";
                Transcript.Add(new ChatTranscriptItem("system", "No fenced code block was found in the latest assistant responses."));
                ApplySuggestedFixCommand.RaiseCanExecuteChanged();
                return;
            }

            var activeContext = await editorContextService.GetActiveContextAsync(CancellationToken.None);
            if (activeContext == null || !activeContext.HasSelection)
            {
                Status = "Selection required";
                Transcript.Add(new ChatTranscriptItem("system", "Select the code region that should receive the fix before applying it."));
                ApplySuggestedFixCommand.RaiseCanExecuteChanged();
                return;
            }

            if (attachedContext != null &&
                !string.IsNullOrWhiteSpace(attachedContext.FilePath) &&
                !string.Equals(attachedContext.FilePath, activeContext.FilePath, System.StringComparison.OrdinalIgnoreCase))
            {
                Status = "File mismatch";
                Transcript.Add(new ChatTranscriptItem("system", "The active editor does not match the file attached to the chat context. Open the analyzed file and select the target region first."));
                ApplySuggestedFixCommand.RaiseCanExecuteChanged();
                return;
            }

            string targetName = string.IsNullOrWhiteSpace(activeContext.FilePath) ? "the current selection" : activeContext.FilePath;
            var decision = MessageBox.Show(
                "Replace the current selection in " + targetName + " with the first suggested code block from the latest assistant response?",
                "Apply Suggested Fix",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);

            if (decision != MessageBoxResult.OK)
            {
                Status = "Apply canceled";
                return;
            }

            await patchService.ApplyPatchAsync(activeContext.FilePath, replacement, CancellationToken.None);
            await TryAcceptLatestResponseAsync("Suggested fix applied and extractor accepted.");
            Status = "Suggested fix applied";
            Transcript.Add(new ChatTranscriptItem("system", "Suggested fix applied to the current selection."));
            ApplySuggestedFixCommand.RaiseCanExecuteChanged();
            MarkResponseHelpfulCommand.RaiseCanExecuteChanged();
        }
        catch (System.Exception error)
        {
            Status = "Apply failed";
            Transcript.Add(new ChatTranscriptItem("system", "Failed to apply suggested fix: " + error.Message));
            ApplySuggestedFixCommand.RaiseCanExecuteChanged();
            MarkResponseHelpfulCommand.RaiseCanExecuteChanged();
        }
    }

    public async Task MarkResponseHelpfulAsync()
    {
        try
        {
            if (!TryGetLatestExtractorId(out int extractorId))
            {
                Status = "No tracked response";
                Transcript.Add(new ChatTranscriptItem("system", "No completed assistant response is available to mark as helpful yet."));
                MarkResponseHelpfulCommand.RaiseCanExecuteChanged();
                return;
            }

            await EnsureConnectedAsync();
            await brokerClient.UpdateExtractorAcceptedAsync(extractorId, CancellationToken.None);
            Status = "Response marked as helpful";
            Transcript.Add(new ChatTranscriptItem("system", "Assistant response marked as helpful."));
            MarkResponseHelpfulCommand.RaiseCanExecuteChanged();
        }
        catch (System.Exception error)
        {
            Status = "Feedback failed";
            Transcript.Add(new ChatTranscriptItem("system", "Failed to mark response as helpful: " + error.Message));
            MarkResponseHelpfulCommand.RaiseCanExecuteChanged();
        }
    }

    private Task ClearAttachedContextAsync()
    {
        attachedContext = null;
        AttachedContextSummary = "No attached selection.";
        ClearAttachedContextCommand.RaiseCanExecuteChanged();
        Status = "Attached context cleared";
        return Task.CompletedTask;
    }

    private Task ClearChatAsync()
    {
        Transcript.Clear();
        requestExtractorIdMap.Clear();
        latestCompletedRequestId = null;
        Status = "Chat cleared";
        ClearChatCommand.RaiseCanExecuteChanged();
        ApplySuggestedFixCommand.RaiseCanExecuteChanged();
        MarkResponseHelpfulCommand.RaiseCanExecuteChanged();
        return Task.CompletedTask;
    }

    private void OnBrokerEventReceived(BrokerEvent brokerEvent)
    {
        void UpdateUi()
        {
            if (brokerEvent.Type == "analysis_chunk")
            {
                ThinkingStatus = string.Empty;
                var last = Transcript.LastOrDefault();
                if (last != null && last.Role == "assistant")
                {
                    last.Content += brokerEvent.Content;
                }
                else
                {
                    Transcript.Add(new ChatTranscriptItem("assistant", brokerEvent.Content));
                }

                Status = "Receiving response...";
                ApplySuggestedFixCommand.RaiseCanExecuteChanged();
                MarkResponseHelpfulCommand.RaiseCanExecuteChanged();
                return;
            }

            if (brokerEvent.Type == "analysis_complete")
            {
                ThinkingStatus = string.Empty;
                latestCompletedRequestId = brokerEvent.RequestId;
                TrackExtractorId(brokerEvent);
                if (!string.IsNullOrWhiteSpace(brokerEvent.Content))
                {
                    var last = Transcript.LastOrDefault();
                    if (last == null || last.Role != "assistant" || last.Content != brokerEvent.Content)
                    {
                        Transcript.Add(new ChatTranscriptItem("assistant", brokerEvent.Content));
                    }
                }

                Status = "Response complete";
                ApplySuggestedFixCommand.RaiseCanExecuteChanged();
                MarkResponseHelpfulCommand.RaiseCanExecuteChanged();
                return;
            }

            if (brokerEvent.Type == "analysis_error" || brokerEvent.Type == "error")
            {
                ThinkingStatus = string.Empty;
                Transcript.Add(new ChatTranscriptItem("system", brokerEvent.Content));
                Status = "Error from broker";
                ApplySuggestedFixCommand.RaiseCanExecuteChanged();
                MarkResponseHelpfulCommand.RaiseCanExecuteChanged();
                return;
            }

            ThinkingStatus = string.Empty;
            Transcript.Add(new ChatTranscriptItem("assistant", brokerEvent.Content));
            Status = "Message received";
            ApplySuggestedFixCommand.RaiseCanExecuteChanged();
            MarkResponseHelpfulCommand.RaiseCanExecuteChanged();
        }

        if (Application.Current?.Dispatcher?.CheckAccess() == true)
        {
            UpdateUi();
            return;
        }

        _ = ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                UpdateUi();
            }
            catch (System.Exception error)
            {
                DiagnosticsLogger.LogError("Unable to update chat UI from broker event: " + error);
            }
        });
    }

    private bool CanApplySuggestedFix()
    {
        return TryGetLatestSuggestedFix(out _);
    }

    private bool CanMarkResponseHelpful()
    {
        return TryGetLatestExtractorId(out _);
    }

    private bool TryGetLatestExtractorId(out int extractorId)
    {
        extractorId = 0;
        string? requestId = latestCompletedRequestId;
        if (requestId == null || requestId.Length == 0)
        {
            return false;
        }

        return requestExtractorIdMap.TryGetValue(requestId, out extractorId) && extractorId > 0;
    }

    private void TrackExtractorId(BrokerEvent brokerEvent)
    {
        try
        {
            using var document = JsonDocument.Parse(brokerEvent.RawPayload);
            if (!document.RootElement.TryGetProperty("payload", out JsonElement payload))
            {
                return;
            }

            if (!payload.TryGetProperty("extractor_id", out JsonElement extractorElement))
            {
                return;
            }

            int extractorId;
            if (extractorElement.ValueKind == JsonValueKind.Number && extractorElement.TryGetInt32(out extractorId))
            {
                requestExtractorIdMap[brokerEvent.RequestId] = extractorId;
                return;
            }

            if (extractorElement.ValueKind == JsonValueKind.String &&
                int.TryParse(extractorElement.GetString(), out extractorId))
            {
                requestExtractorIdMap[brokerEvent.RequestId] = extractorId;
            }
        }
        catch (JsonException)
        {
            // Ignore malformed payloads and keep the chat usable.
        }
    }

    private async Task TryAcceptLatestResponseAsync(string successMessage)
    {
        if (!TryGetLatestExtractorId(out int extractorId))
        {
            return;
        }

        await EnsureConnectedAsync();
        await brokerClient.UpdateExtractorAcceptedAsync(extractorId, CancellationToken.None);
        Transcript.Add(new ChatTranscriptItem("system", successMessage));
    }

    private bool TryGetLatestSuggestedFix(out string replacement)
    {
        foreach (var item in Transcript.Reverse())
        {
            if (item.Role != "assistant")
            {
                continue;
            }

            if (TryExtractFirstCodeFence(item.Content, out replacement))
            {
                return true;
            }
        }

        replacement = string.Empty;
        return false;
    }

    private static bool TryExtractFirstCodeFence(string markdown, out string replacement)
    {
        replacement = string.Empty;
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return false;
        }

        int fenceStart = markdown.IndexOf("```", System.StringComparison.Ordinal);
        if (fenceStart < 0)
        {
            return false;
        }

        int contentStart = markdown.IndexOf('\n', fenceStart);
        if (contentStart < 0)
        {
            return false;
        }

        int fenceEnd = markdown.IndexOf("```", contentStart + 1, System.StringComparison.Ordinal);
        if (fenceEnd < 0)
        {
            return false;
        }

        replacement = markdown.Substring(contentStart + 1, fenceEnd - contentStart - 1);
        return !string.IsNullOrWhiteSpace(replacement);
    }

    private async Task EnsureConnectedAsync()
    {
        if (brokerClient.IsConnected)
        {
            return;
        }

        await connectionLock.WaitAsync();
        try
        {
            if (brokerClient.IsConnected)
            {
                return;
            }

            await brokerClient.ConnectAsync(
                new BrokerConnectionOptions
                {
                    Endpoint = ConvisoOptions.DefaultBrokerEndpoint,
                    ApiKey = settingsService.GetSecret(ConvisoOptions.ApiTokenKey, string.Empty),
                },
                CancellationToken.None);
        }
        finally
        {
            connectionLock.Release();
        }
    }

    private static string BuildBrokerMessage(string message, EditorContextSnapshot? context)
    {
        if (context == null)
        {
            return message;
        }

        string attachedContent = context.HasSelection
            ? context.SelectionText
            : TruncateContext(context.DocumentText, 12000);

        if (string.IsNullOrWhiteSpace(attachedContent))
        {
            return message;
        }

        string referenceLabel = string.IsNullOrWhiteSpace(context.FilePath) ? "Active editor" : context.FilePath;
        string contextKind = context.HasSelection ? "selection" : "file";

        return string.Join(
            "\n",
            "Attached IDE context:",
            string.Empty,
            $"Context type: {contextKind}",
            $"Reference: {referenceLabel}",
            $"Language: {context.Language}",
            string.Empty,
            "```" + context.Language,
            attachedContent,
            "```",
            string.Empty,
            "Message:",
            message);
    }

    private static string TruncateContext(string content, int maxLength)
    {
        string normalized = (content ?? string.Empty).Trim();
        if (normalized.Length <= maxLength)
        {
            return normalized;
        }

        return normalized.Substring(0, maxLength) +
               "\n\n[truncated: original content exceeded " + maxLength + " characters]";
    }

    private static string BuildAttachedContextSummary(EditorContextSnapshot context)
    {
        string referenceLabel = string.IsNullOrWhiteSpace(context.FilePath) ? "Active editor" : context.FilePath;
        string typeLabel = context.HasSelection ? "Selection" : "File";
        return typeLabel + " attached: " + referenceLabel;
    }

    private static string BuildSimilarIssuesMessage(
        EditorContextSnapshot selectionContext,
        WorkspaceContextSnapshot workspaceContext)
    {
        string workspaceFiles = string.Join(
            "\n\n---\n\n",
            workspaceContext.Files.Select(file =>
                string.Join(
                    "\n",
                    "File: " + file.FilePath,
                    "Language: " + file.Language,
                    string.Empty,
                    "```" + file.Language,
                    file.Content,
                    "```")));

        return string.Join(
            "\n",
            "Operation: similar_issues_scan",
            "Reference selection:",
            string.Empty,
            "File: " + selectionContext.FilePath,
            "Language: " + selectionContext.Language,
            string.Empty,
            "```" + selectionContext.Language,
            selectionContext.SelectionText,
            "```",
            string.Empty,
            "Attached analysis and workspace context:",
            string.Empty,
            "Workspace context:",
            workspaceFiles);
    }
}
}
