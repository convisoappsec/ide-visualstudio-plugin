using System.Text.Json;

namespace Conviso.Platform.VisualStudio.Services.Platform
{
    internal static class JsonExtensions
    {
        public static string GetPropertyOrDefault(this JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }
    }
}
