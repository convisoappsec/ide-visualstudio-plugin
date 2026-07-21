using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Conviso.Platform.VisualStudio.Configuration;
using Conviso.Platform.VisualStudio.Infrastructure;
using Conviso.Platform.VisualStudio.Services.Broker;
using Conviso.Platform.VisualStudio.Models;
using Conviso.Platform.VisualStudio.Services.Platform;

namespace Conviso.Platform.VisualStudio.ViewModels
{
    internal sealed class SettingsToolWindowViewModel : ObservableObject
    {
        private readonly ISettingsService settingsService;
        private readonly IPlatformFacade platformFacade;
        private string apiBaseUrl;
        private string apiToken;
        private string companyId;
        private string requirementsScopeId;
        private string brokerEndpoint;
        private string brokerApiKey;
        private AccessibleCompanyOption? selectedCompany;
        private string status = "Ready";

        public SettingsToolWindowViewModel(ISettingsService settingsService, IPlatformFacade platformFacade)
        {
            this.settingsService = settingsService;
            this.platformFacade = platformFacade;
            apiBaseUrl = ConvisoOptions.DefaultApiBaseUrl;
            apiToken = settingsService.GetSecret(ConvisoOptions.ApiTokenKey, string.Empty);
            companyId = settingsService.GetString(ConvisoOptions.CompanyIdKey, string.Empty);
            requirementsScopeId = settingsService.GetString(ConvisoOptions.RequirementsScopeIdKey, string.Empty);
            brokerEndpoint = ConvisoOptions.DefaultBrokerEndpoint;
            brokerApiKey = apiToken;
            Companies = new ObservableCollection<AccessibleCompanyOption>();
            SaveCommand = new AsyncDelegateCommand(SaveAsync);
            TestApiCommand = new AsyncDelegateCommand(TestApiAsync);
            TestBrokerCommand = new AsyncDelegateCommand(TestBrokerAsync);
            ResetDefaultsCommand = new AsyncDelegateCommand(ResetDefaultsAsync);
            LoadCompaniesCommand = new AsyncDelegateCommand(LoadCompaniesAsync);
            ApplySelectedCompanyCommand = new AsyncDelegateCommand(ApplySelectedCompanyAsync, () => SelectedCompany != null);
        }

        public string ApiBaseUrl { get => apiBaseUrl; set => SetProperty(ref apiBaseUrl, value); }
        public string ApiToken { get => apiToken; set => SetProperty(ref apiToken, value); }
        public string CompanyId { get => companyId; set => SetProperty(ref companyId, value); }
        public string RequirementsScopeId { get => requirementsScopeId; set => SetProperty(ref requirementsScopeId, value); }
        public string BrokerEndpoint { get => brokerEndpoint; set => SetProperty(ref brokerEndpoint, value); }
        public string BrokerApiKey { get => brokerApiKey; set => SetProperty(ref brokerApiKey, value); }
        public string Status { get => status; set => SetProperty(ref status, value); }

        public ObservableCollection<AccessibleCompanyOption> Companies { get; }

        public AccessibleCompanyOption? SelectedCompany
        {
            get => selectedCompany;
            set
            {
                if (SetProperty(ref selectedCompany, value))
                {
                    ApplySelectedCompanyCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public AsyncDelegateCommand SaveCommand { get; }

        public AsyncDelegateCommand TestApiCommand { get; }

        public AsyncDelegateCommand TestBrokerCommand { get; }

        public AsyncDelegateCommand ResetDefaultsCommand { get; }

        public AsyncDelegateCommand LoadCompaniesCommand { get; }

        public AsyncDelegateCommand ApplySelectedCompanyCommand { get; }

        private Task SaveAsync()
        {
            string normalizedApiBaseUrl = ConvisoOptions.DefaultApiBaseUrl;
            string normalizedBrokerEndpoint = ConvisoOptions.DefaultBrokerEndpoint;
            string normalizedCompanyId = CompanyId.Trim();
            string normalizedRequirementsScopeId = string.IsNullOrWhiteSpace(RequirementsScopeId)
                ? normalizedCompanyId
                : RequirementsScopeId.Trim();
            string normalizedApiToken = ApiToken.Trim();
            string normalizedBrokerApiKey = normalizedApiToken;

            ApiBaseUrl = normalizedApiBaseUrl;
            BrokerEndpoint = normalizedBrokerEndpoint;
            CompanyId = normalizedCompanyId;
            RequirementsScopeId = normalizedRequirementsScopeId;
            BrokerApiKey = normalizedBrokerApiKey;

            settingsService.SetSecret(ConvisoOptions.ApiTokenKey, normalizedApiToken);
            settingsService.SetString(ConvisoOptions.CompanyIdKey, normalizedCompanyId);
            settingsService.SetString(ConvisoOptions.RequirementsScopeIdKey, normalizedRequirementsScopeId);
            Status = "Settings saved. Secrets are stored with Windows user protection.";
            return Task.CompletedTask;
        }

        private async Task TestApiAsync()
        {
            string normalizedApiBaseUrl = ConvisoOptions.DefaultApiBaseUrl;
            string normalizedApiToken = ApiToken.Trim();
            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, normalizedApiBaseUrl.TrimEnd('/') + "/graphql");
            request.Headers.Add("Accept", "application/json");
            if (!string.IsNullOrWhiteSpace(normalizedApiToken))
            {
                request.Headers.Add("Authorization", "Bearer " + normalizedApiToken);
                request.Headers.Add("x-api-key", normalizedApiToken);
            }

            request.Content = new StringContent(
                "{\"query\":\"query { __typename }\",\"variables\":{}}",
                Encoding.UTF8,
                "application/json");

            try
            {
                Status = "Testing API...";
                using HttpResponseMessage response = await httpClient.SendAsync(request, CancellationToken.None);
                string body = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    Status = body.IndexOf("\"errors\"", StringComparison.OrdinalIgnoreCase) >= 0
                        ? "API reached, but GraphQL returned errors. Check token and permissions."
                        : "API connection successful.";
                    return;
                }

                Status = "API test failed: HTTP " + (int)response.StatusCode;
            }
            catch (Exception error)
            {
                Status = "API test failed: " + error.Message;
            }
        }

        private async Task TestBrokerAsync()
        {
            var brokerClient = new BrokerClient();
            try
            {
                Status = "Testing broker...";
                await brokerClient.ConnectAsync(
                    new BrokerConnectionOptions
                    {
                        Endpoint = ConvisoOptions.DefaultBrokerEndpoint,
                        ApiKey = ApiToken.Trim(),
                    },
                    CancellationToken.None);
                Status = "Broker connection successful.";
            }
            catch (Exception error)
            {
                Status = "Broker test failed: " + error.Message;
            }
            finally
            {
                await brokerClient.DisconnectAsync(CancellationToken.None);
            }
        }

        private Task ResetDefaultsAsync()
        {
            ApiBaseUrl = ConvisoOptions.DefaultApiBaseUrl;
            BrokerEndpoint = ConvisoOptions.DefaultBrokerEndpoint;
            if (string.IsNullOrWhiteSpace(RequirementsScopeId))
            {
                RequirementsScopeId = CompanyId.Trim();
            }

            Status = "Defaults restored for API and broker endpoint.";
            return Task.CompletedTask;
        }

        private async Task LoadCompaniesAsync()
        {
            try
            {
                await SaveAsync();
                Status = "Loading accessible companies...";
                var companies = await platformFacade.GetAccessibleCompaniesAsync(CancellationToken.None);
                Companies.Clear();
                foreach (AccessibleCompanyOption company in companies)
                {
                    Companies.Add(company);
                }

                string currentCompanyId = CompanyId.Trim();
                SelectedCompany = Companies.FirstOrDefault(item => item.Id == currentCompanyId) ?? Companies.FirstOrDefault();
                Status = Companies.Count == 0
                    ? "No accessible companies returned for this API key."
                    : $"Loaded {Companies.Count} accessible compan{(Companies.Count == 1 ? "y" : "ies")}.";
            }
            catch (Exception error)
            {
                Status = "Company discovery failed: " + error.Message;
            }
        }

        private Task ApplySelectedCompanyAsync()
        {
            if (SelectedCompany == null)
            {
                Status = "Select a company first.";
                return Task.CompletedTask;
            }

            CompanyId = SelectedCompany.Id;
            RequirementsScopeId = SelectedCompany.Id;
            Status = $"Selected company {SelectedCompany.DisplayLabel}. Save settings to persist it.";
            return Task.CompletedTask;
        }

        private static string NormalizeApiBaseUrl(string value)
        {
            string trimmed = value?.Trim().TrimEnd('/') ?? string.Empty;
            return string.IsNullOrWhiteSpace(trimmed) ? ConvisoOptions.DefaultApiBaseUrl : trimmed;
        }

        private static string NormalizeBrokerEndpoint(string value)
        {
            string trimmed = value?.Trim().TrimEnd('/') ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return ConvisoOptions.DefaultBrokerEndpoint;
            }

            if (trimmed.EndsWith("/cable", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Substring(0, trimmed.Length - "/cable".Length) + "/ws";
            }

            if (trimmed.EndsWith("/ws", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            return trimmed + "/ws";
        }
    }
}
