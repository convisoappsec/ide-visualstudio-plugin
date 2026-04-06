using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Conviso.Platform.VisualStudio.Configuration;

namespace Conviso.Platform.VisualStudio.Services.Platform
{
    internal sealed class PlatformApiClient : IPlatformApiClient
    {
        private readonly ISettingsService settingsService;
        private readonly HttpClient httpClient;

        public PlatformApiClient(ISettingsService settingsService)
        {
            this.settingsService = settingsService;
            httpClient = new HttpClient();
        }

        public async Task<string> QueryAsync(string graphqlDocument, string variablesJson, CancellationToken cancellationToken)
        {
            string apiBaseUrl = settingsService.GetString(ConvisoOptions.ApiBaseUrlKey, ConvisoOptions.DefaultApiBaseUrl);
            string apiToken = settingsService.GetSecret(ConvisoOptions.ApiTokenKey, string.Empty);

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{apiBaseUrl.TrimEnd('/')}/graphql");
            request.Headers.Add("Accept", "application/json");

            if (!string.IsNullOrWhiteSpace(apiToken))
            {
                request.Headers.Add("Authorization", $"Bearer {apiToken}");
            }

            string payload = "{\"query\":" + JsonString(graphqlDocument) + ",\"variables\":" + variablesJson + "}";
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        private static string JsonString(string value)
        {
            return System.Text.Json.JsonSerializer.Serialize(value);
        }
    }
}
