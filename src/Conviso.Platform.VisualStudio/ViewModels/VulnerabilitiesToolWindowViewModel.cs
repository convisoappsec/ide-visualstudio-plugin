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
    private AccessibleCompanyOption? selectedCompany;
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
        Companies = new ObservableCollection<AccessibleCompanyOption>();
        Assets = new ObservableCollection<AssetOption>();
        Details = new VulnerabilityDetailsViewModel(platformFacade, settingsService, brokerClient);
        RefreshCommand = new AsyncDelegateCommand(RefreshAsync);
        _ = LoadFilterOptionsAsync();
    }

    public ObservableCollection<VulnerabilitySummary> Items { get; }

    public ObservableCollection<AccessibleCompanyOption> Companies { get; }

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
                _ = Details.LoadAsync(value);
            }
        }
    }

    public AccessibleCompanyOption? SelectedCompany
    {
        get => selectedCompany;
        set
        {
            if (SetProperty(ref selectedCompany, value))
            {
                PersistSelectedCompany(value);
                _ = LoadAssetsForSelectedCompanyAsync();
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
        foreach (var item in await platformFacade.GetVulnerabilitiesAsync(
            SelectedCompany?.Id,
            string.IsNullOrWhiteSpace(SelectedAsset?.Id) ? null : SelectedAsset?.Id,
            CancellationToken.None))
        {
            Items.Add(item);
        }

        Status = $"Loaded {Items.Count} item(s)";
    }

    private async Task LoadFilterOptionsAsync()
    {
        try
        {
            var companies = await platformFacade.GetAccessibleCompaniesAsync(CancellationToken.None);
            Companies.Clear();
            foreach (AccessibleCompanyOption company in companies.OrderBy(item => item.Label))
            {
                Companies.Add(company);
            }

            string currentCompanyId = settingsService.GetString(ConvisoOptions.CompanyIdKey, string.Empty);
            SelectedCompany = Companies.FirstOrDefault(item => item.Id == currentCompanyId) ?? Companies.FirstOrDefault();
        }
        catch
        {
            string currentCompanyId = settingsService.GetString(ConvisoOptions.CompanyIdKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(currentCompanyId))
            {
                Companies.Clear();
                Companies.Add(new AccessibleCompanyOption { Id = currentCompanyId, Label = currentCompanyId });
                SelectedCompany = Companies[0];
            }
        }

        await RefreshAsync();
    }

    private async Task LoadAssetsForSelectedCompanyAsync()
    {
        Assets.Clear();
        Assets.Add(new AssetOption { Id = string.Empty, Name = "All assets" });
        SelectedAsset = Assets[0];

        if (SelectedCompany == null)
        {
            return;
        }

        try
        {
            var assets = await platformFacade.GetAssetsAsync(SelectedCompany.Id, CancellationToken.None);
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

    private void PersistSelectedCompany(AccessibleCompanyOption? company)
    {
        if (company == null || string.IsNullOrWhiteSpace(company.Id))
        {
            return;
        }

        settingsService.SetString(ConvisoOptions.CompanyIdKey, company.Id);
        settingsService.SetString(ConvisoOptions.RequirementsScopeIdKey, company.Id);
    }
}
}
