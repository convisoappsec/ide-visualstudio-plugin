using System.Collections.ObjectModel;
using System.Linq;
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
    private string companyId = string.Empty;
    private AssetOption? selectedAsset;
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
        Assets = new ObservableCollection<AssetOption>();
        Details = new VulnerabilityDetailsViewModel(platformFacade, settingsService, brokerClient);
        RefreshCommand = new AsyncDelegateCommand(RefreshAsync);
    }

    public ObservableCollection<VulnerabilitySummary> Items { get; }

    public ObservableCollection<AssetOption> Assets { get; }

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
                _ = LoadDetailsSafelyAsync(value);
            }
        }
    }

    public AssetOption? SelectedAsset
    {
        get => selectedAsset;
        set => SetProperty(ref selectedAsset, value);
    }

    public AsyncDelegateCommand RefreshCommand { get; }

    public async Task RefreshAsync()
    {
        Status = "Loading vulnerabilities...";
        Items.Clear();
        try
        {
            string currentCompanyId = settingsService.GetString(ConvisoOptions.CompanyIdKey, string.Empty);
            if (companyId != currentCompanyId)
            {
                companyId = currentCompanyId;
                await LoadAssetsForSelectedCompanyAsync();
            }

            foreach (var item in await platformFacade.GetVulnerabilitiesAsync(
                companyId,
                string.IsNullOrWhiteSpace(SelectedAsset?.Id) ? null : SelectedAsset?.Id,
                CancellationToken.None))
            {
                Items.Add(item);
            }

            Status = $"Loaded {Items.Count} item(s)";
        }
        catch (System.Exception error)
        {
            Status = "Unable to load vulnerabilities: " + error.Message;
            DiagnosticsLogger.LogError("Unable to load vulnerabilities: " + error);
        }
    }

    public Task InitializeAsync()
    {
        return LoadFilterOptionsAsync();
    }

    private async Task LoadDetailsSafelyAsync(VulnerabilitySummary? item)
    {
        try
        {
            await Details.LoadAsync(item);
        }
        catch (System.Exception error)
        {
            Status = "Unable to load vulnerability details: " + error.Message;
            DiagnosticsLogger.LogError("Unable to load vulnerability details: " + error);
        }
    }

    private async Task LoadFilterOptionsAsync()
    {
        companyId = settingsService.GetString(ConvisoOptions.CompanyIdKey, string.Empty);
        await LoadAssetsForSelectedCompanyAsync();
        await RefreshAsync();
    }

    private async Task LoadAssetsForSelectedCompanyAsync()
    {
        Assets.Clear();
        Assets.Add(new AssetOption { Id = string.Empty, Name = "All assets" });
        SelectedAsset = Assets[0];

        if (string.IsNullOrWhiteSpace(companyId))
        {
            return;
        }

        try
        {
            var assets = await platformFacade.GetAssetsAsync(companyId, CancellationToken.None);
            foreach (AssetOption asset in assets.OrderBy(item => item.Name))
            {
                Assets.Add(asset);
            }
        }
        catch
        {
            // Keep the "All assets" option even if asset loading fails.
        }
    }

}
}
