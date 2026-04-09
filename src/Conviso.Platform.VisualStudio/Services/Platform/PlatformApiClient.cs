using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Conviso.Platform.VisualStudio.Configuration;
using Conviso.Platform.VisualStudio.Infrastructure;

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
            string endpoint = $"{apiBaseUrl.TrimEnd('/')}/graphql";

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("Accept", "application/json");

            if (!string.IsNullOrWhiteSpace(apiToken))
            {
                request.Headers.Add("Authorization", $"Bearer {apiToken}");
                request.Headers.Add("x-api-key", apiToken);
            }

            string payload = "{\"query\":" + JsonString(graphqlDocument) + ",\"variables\":" + variablesJson + "}";
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                LogGraphQlFailure(endpoint, (int)response.StatusCode, graphqlDocument, variablesJson, responseBody);
                throw new HttpRequestException($"GraphQL request failed: HTTP {(int)response.StatusCode}. See Visual Studio ActivityLog for request details.");
            }

            if (TryExtractGraphQlErrors(responseBody, out string? graphQlMessage))
            {
                LogGraphQlFailure(endpoint, (int)response.StatusCode, graphqlDocument, variablesJson, responseBody);
                throw new HttpRequestException(graphQlMessage ?? "GraphQL returned errors. See Visual Studio ActivityLog for request details.");
            }

            return responseBody;
        }

        private static string JsonString(string value)
        {
            return System.Text.Json.JsonSerializer.Serialize(value);
        }

        private static bool TryExtractGraphQlErrors(string responseBody, out string? message)
        {
            message = null;

            try
            {
                using JsonDocument document = JsonDocument.Parse(responseBody);
                if (!document.RootElement.TryGetProperty("errors", out JsonElement errors) ||
                    errors.ValueKind != JsonValueKind.Array)
                {
                    return false;
                }

                var builder = new StringBuilder();
                foreach (JsonElement item in errors.EnumerateArray())
                {
                    if (!item.TryGetProperty("message", out JsonElement messageElement))
                    {
                        continue;
                    }

                    string? value = messageElement.GetString();
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        continue;
                    }

                    if (builder.Length > 0)
                    {
                        builder.Append("; ");
                    }

                    builder.Append(value);
                }

                message = builder.Length > 0 ? builder.ToString() : "GraphQL returned errors.";
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static void LogGraphQlFailure(string endpoint, int statusCode, string graphqlDocument, string variablesJson, string responseBody)
        {
            string operationName = ExtractOperationName(graphqlDocument);
            DiagnosticsLogger.LogError(
                $"GraphQL failure\n" +
                $"Operation: {operationName}\n" +
                $"Endpoint: {endpoint}\n" +
                $"HTTP status: {statusCode}\n" +
                $"Variables: {variablesJson}\n" +
                $"Query:\n{graphqlDocument.Trim()}\n" +
                $"Response:\n{(string.IsNullOrWhiteSpace(responseBody) ? "(empty response body)" : responseBody)}");
        }

        private static string ExtractOperationName(string graphqlDocument)
        {
            string normalized = graphqlDocument.Replace("\r", " ").Replace("\n", " ").Trim();
            string[] parts = normalized.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && (parts[0] == "query" || parts[0] == "mutation"))
            {
                return parts[1];
            }

            return "anonymous";
        }
    }
}
