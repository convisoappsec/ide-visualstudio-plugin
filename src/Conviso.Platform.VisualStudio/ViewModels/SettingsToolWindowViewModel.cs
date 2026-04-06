using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Conviso.Platform.VisualStudio.Configuration;
using Conviso.Platform.VisualStudio.Infrastructure;
using Conviso.Platform.VisualStudio.Services.Broker;

namespace Conviso.Platform.VisualStudio.ViewModels
{
    internal sealed class SettingsToolWindowViewModel : ObservableObject
    {
        private readonly ISettingsService settingsService;
        private string apiBaseUrl;
        private string apiToken;
        private string companyId;
        private string requirementsScopeId;
        private string brokerEndpoint;
        private string brokerApiKey;
        private string status = "Ready";

        public SettingsToolWindowViewModel(ISettingsService settingsService)
        {
            this.settingsService = settingsService;
            apiBaseUrl = settingsService.GetString(ConvisoOptions.ApiBaseUrlKey, ConvisoOptions.DefaultApiBaseUrl);
            apiToken = settingsService.GetSecret(ConvisoOptions.ApiTokenKey, string.Empty);
            companyId = settingsService.GetString(ConvisoOptions.CompanyIdKey, string.Empty);
            requirementsScopeId = settingsService.GetString(ConvisoOptions.RequirementsScopeIdKey, string.Empty);
            brokerEndpoint = settingsService.GetString(ConvisoOptions.BrokerEndpointKey, ConvisoOptions.DefaultBrokerEndpoint);
            brokerApiKey = settingsService.GetSecret(ConvisoOptions.BrokerApiKeyKey, string.Empty);
            SaveCommand = new AsyncDelegateCommand(SaveAsync);
            TestApiCommand = new AsyncDelegateCommand(TestApiAsync);
            TestBrokerCommand = new AsyncDelegateCommand(TestBrokerAsync);
            ResetDefaultsCommand = new AsyncDelegateCommand(ResetDefaultsAsync);
        }

        public string ApiBaseUrl { get => apiBaseUrl; set => SetProperty(ref apiBaseUrl, value); }
        public string ApiToken { get => apiToken; set => SetProperty(ref apiToken, value); }
        public string CompanyId { get => companyId; set => SetProperty(ref companyId, value); }
        public string RequirementsScopeId { get => requirementsScopeId; set => SetProperty(ref requirementsScopeId, value); }
        public string BrokerEndpoint { get => brokerEndpoint; set => SetProperty(ref brokerEndpoint, value); }
        public string BrokerApiKey { get => brokerApiKey; set => SetProperty(ref brokerApiKey, value); }
        public string Status { get => status; set => SetProperty(ref status, value); }

        public AsyncDelegateCommand SaveCommand { get; }

        public AsyncDelegateCommand TestApiCommand { get; }

        public AsyncDelegateCommand TestBrokerCommand { get; }

        public AsyncDelegateCommand ResetDefaultsCommand { get; }

        private Task SaveAsync()
        {
            string normalizedApiBaseUrl = NormalizeApiBaseUrl(ApiBaseUrl);
            string normalizedBrokerEndpoint = NormalizeBrokerEndpoint(BrokerEndpoint);
            string normalizedCompanyId = CompanyId.Trim();
            string normalizedRequirementsScopeId = string.IsNullOrWhiteSpace(RequirementsScopeId)
                ? normalizedCompanyId
                : RequirementsScopeId.Trim();

            ApiBaseUrl = normalizedApiBaseUrl;
            BrokerEndpoint = normalizedBrokerEndpoint;
            CompanyId = normalizedCompanyId;
            RequirementsScopeId = normalizedRequirementsScopeId;

            settingsService.SetString(ConvisoOptions.ApiBaseUrlKey, normalizedApiBaseUrl);
            settingsService.SetSecret(ConvisoOptions.ApiTokenKey, ApiToken.Trim());
            settingsService.SetString(ConvisoOptions.CompanyIdKey, normalizedCompanyId);
            settingsService.SetString(ConvisoOptions.RequirementsScopeIdKey, normalizedRequirementsScopeId);
            settingsService.SetString(ConvisoOptions.BrokerEndpointKey, normalizedBrokerEndpoint);
            settingsService.SetSecret(ConvisoOptions.BrokerApiKeyKey, BrokerApiKey.Trim());
            Status = "Settings saved. Secrets are stored with Windows user protection.";
            return Task.CompletedTask;
        }

        private async Task TestApiAsync()
        {
            string normalizedApiBaseUrl = NormalizeApiBaseUrl(ApiBaseUrl);
            string normalizedApiToken = ApiToken.Trim();
            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, normalizedApiBaseUrl.TrimEnd('/') + "/graphql");
            request.Headers.Add("Accept", "application/json");
            if (!string.IsNullOrWhiteSpace(normalizedApiToken))
            {
                request.Headers.Add("Authorization", "Bearer " + normalizedApiToken);
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
                        Endpoint = NormalizeBrokerEndpoint(BrokerEndpoint),
                        ApiKey = BrokerApiKey.Trim(),
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
