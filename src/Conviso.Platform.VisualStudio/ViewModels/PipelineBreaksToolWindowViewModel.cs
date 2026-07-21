using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Conviso.Platform.VisualStudio.Infrastructure;
using Conviso.Platform.VisualStudio.Models;
using Conviso.Platform.VisualStudio.Services.Platform;

namespace Conviso.Platform.VisualStudio.ViewModels
{
internal sealed class PipelineBreaksToolWindowViewModel : ObservableObject
{
    private readonly IPlatformFacade platformFacade;
    private PipelineBreakSummary? selectedItem;
    private string status = "Ready";
    private string detailStatus = string.Empty;
    private string detailExecutionDate = string.Empty;
    private string detailTriggeredBy = string.Empty;
    private string detailSource = string.Empty;
    private string detailAssetName = string.Empty;
    private string detailReasonText = string.Empty;
    private string detailCommandStatus = "Select a pipeline break";

    public PipelineBreaksToolWindowViewModel(IPlatformFacade platformFacade)
    {
        this.platformFacade = platformFacade;
        Items = new ObservableCollection<PipelineBreakSummary>();
        RefreshCommand = new AsyncDelegateCommand(RefreshAsync);
    }

    public ObservableCollection<PipelineBreakSummary> Items { get; }

    public string Status
    {
        get => status;
        set => SetProperty(ref status, value);
    }

    public PipelineBreakSummary? SelectedItem
    {
        get => selectedItem;
        set
        {
            if (SetProperty(ref selectedItem, value))
            {
                _ = LoadDetailsAsync(value);
            }
        }
    }

    public string DetailStatus { get => detailStatus; set => SetProperty(ref detailStatus, value); }

    public string DetailExecutionDate { get => detailExecutionDate; set => SetProperty(ref detailExecutionDate, value); }

    public string DetailTriggeredBy { get => detailTriggeredBy; set => SetProperty(ref detailTriggeredBy, value); }

    public string DetailSource { get => detailSource; set => SetProperty(ref detailSource, value); }

    public string DetailAssetName { get => detailAssetName; set => SetProperty(ref detailAssetName, value); }

    public string DetailReasonText { get => detailReasonText; set => SetProperty(ref detailReasonText, value); }

    public string DetailCommandStatus { get => detailCommandStatus; set => SetProperty(ref detailCommandStatus, value); }

    public AsyncDelegateCommand RefreshCommand { get; }

    public async Task RefreshAsync()
    {
        Status = "Loading pipeline breaks...";
        Items.Clear();
        SelectedItem = null;
        ResetDetails();
        try
        {
            foreach (PipelineBreakSummary item in await platformFacade.GetPipelineBreaksAsync(CancellationToken.None))
            {
                Items.Add(item);
            }
            Status = $"Loaded {Items.Count} item(s)";
        }
        catch (System.Exception error)
        {
            Status = "Unable to load pipeline breaks: " + error.Message;
            DiagnosticsLogger.LogError("Unable to load pipeline breaks: " + error);
        }
    }

    private async Task LoadDetailsAsync(PipelineBreakSummary? item)
    {
        if (item == null)
        {
            ResetDetails();
            return;
        }

        DetailCommandStatus = "Loading pipeline break details...";
        PipelineBreakDetails details;
        try
        {
            details = await platformFacade.GetPipelineBreakDetailsAsync(item.Id, CancellationToken.None);
        }
        catch (System.Exception error)
        {
            DetailCommandStatus = "Unable to load details: " + error.Message;
            DiagnosticsLogger.LogError("Unable to load pipeline break details: " + error);
            return;
        }
        DetailStatus = details.Status;
        DetailExecutionDate = details.ExecutionDate;
        DetailTriggeredBy = details.TriggeredBy;
        DetailSource = details.Source;
        DetailAssetName = details.AssetName;
        DetailReasonText = details.ReasonText;
        DetailCommandStatus = "Pipeline break details loaded";
    }

    private void ResetDetails()
    {
        DetailStatus = string.Empty;
        DetailExecutionDate = string.Empty;
        DetailTriggeredBy = string.Empty;
        DetailSource = string.Empty;
        DetailAssetName = string.Empty;
        DetailReasonText = string.Empty;
        DetailCommandStatus = "Select a pipeline break";
    }
}
}
