using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Conviso.Platform.VisualStudio.Configuration;
using Conviso.Platform.VisualStudio.Infrastructure;
using Conviso.Platform.VisualStudio.Models;
using Conviso.Platform.VisualStudio.Services.Broker;
using Conviso.Platform.VisualStudio.Services.Platform;

namespace Conviso.Platform.VisualStudio.ViewModels
{
internal sealed class VulnerabilitiesToolWindowViewModel : ObservableObject
{
    private readonly IPlatformFacade platformFacade;
    private readonly ISettingsService settingsService;
    private readonly IBrokerClient brokerClient;
    private VulnerabilitySummary? selectedItem;
    private string status = "Ready";

    public VulnerabilitiesToolWindowViewModel(
        IPlatformFacade platformFacade,
        ISettingsService settingsService,
        IBrokerClient brokerClient)
    {
        this.platformFacade = platformFacade;
        this.settingsService = settingsService;
        this.brokerClient = brokerClient;
        Items = new ObservableCollection<VulnerabilitySummary>();
        Details = new VulnerabilityDetailsViewModel(platformFacade, settingsService, brokerClient);
        RefreshCommand = new AsyncDelegateCommand(RefreshAsync);
    }

    public ObservableCollection<VulnerabilitySummary> Items { get; }

    public VulnerabilityDetailsViewModel Details { get; }

    public string Status
    {
        get => status;
        set => SetProperty(ref status, value);
    }

    public VulnerabilitySummary? SelectedItem
    {
        get => selectedItem;
        set
        {
            if (SetProperty(ref selectedItem, value))
            {
                _ = Details.LoadAsync(value);
            }
        }
    }

    public AsyncDelegateCommand RefreshCommand { get; }

    public async Task RefreshAsync()
    {
        Status = "Loading vulnerabilities...";
        Items.Clear();
        foreach (var item in await platformFacade.GetVulnerabilitiesAsync(CancellationToken.None))
        {
            Items.Add(item);
        }

        Status = $"Loaded {Items.Count} item(s)";
    }
}
}
