using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;

namespace Conviso.Platform.VisualStudio.Configuration
{
    internal sealed class SettingsService : ISettingsService
    {
        private readonly WritableSettingsStore settingsStore;

        public SettingsService(IServiceProvider serviceProvider)
        {
            var shellSettingsManager = new ShellSettingsManager(serviceProvider);
            settingsStore = shellSettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!settingsStore.CollectionExists(ConvisoOptions.CollectionPath))
            {
                settingsStore.CreateCollection(ConvisoOptions.CollectionPath);
            }
        }

        public string GetString(string key, string defaultValue = "")
        {
            return settingsStore.PropertyExists(ConvisoOptions.CollectionPath, key)
                ? settingsStore.GetString(ConvisoOptions.CollectionPath, key, defaultValue)
                : defaultValue;
        }

        public void SetString(string key, string value)
        {
            settingsStore.SetString(ConvisoOptions.CollectionPath, key, value ?? string.Empty);
        }

        public string GetSecret(string key, string defaultValue = "")
        {
            if (!settingsStore.PropertyExists(ConvisoOptions.CollectionPath, key))
            {
                return defaultValue;
            }

            string storedValue = settingsStore.GetString(ConvisoOptions.CollectionPath, key, string.Empty);
            if (string.IsNullOrWhiteSpace(storedValue))
            {
                return defaultValue;
            }

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(storedValue);
                byte[] decryptedBytes = ProtectedData.Unprotect(
                    encryptedBytes,
                    GetEntropy(key),
                    DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (FormatException)
            {
                // Backward-compatible fallback for secrets previously stored in plain text.
                return storedValue;
            }
            catch (CryptographicException)
            {
                return defaultValue;
            }
        }

        public void SetSecret(string key, string value)
        {
            string normalized = value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
            {
                settingsStore.SetString(ConvisoOptions.CollectionPath, key, string.Empty);
                return;
            }

            byte[] plainBytes = Encoding.UTF8.GetBytes(normalized);
            byte[] encryptedBytes = ProtectedData.Protect(
                plainBytes,
                GetEntropy(key),
                DataProtectionScope.CurrentUser);

            settingsStore.SetString(
                ConvisoOptions.CollectionPath,
                key,
                Convert.ToBase64String(encryptedBytes));
        }

        private static byte[] GetEntropy(string key)
        {
            return Encoding.UTF8.GetBytes(ConvisoOptions.CollectionPath + ":" + key);
        }
    }
}
