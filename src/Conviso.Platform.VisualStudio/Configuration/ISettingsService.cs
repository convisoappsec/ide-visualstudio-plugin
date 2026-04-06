namespace Conviso.Platform.VisualStudio.Configuration
{
    public interface ISettingsService
    {
        string GetString(string key, string defaultValue = "");

        void SetString(string key, string value);

        string GetSecret(string key, string defaultValue = "");

        void SetSecret(string key, string value);
    }
}
