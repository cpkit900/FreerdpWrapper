using System;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace FreeRdpWrapper.Services
{
    public class AppSettings
    {
        public string MasterPasswordHash { get; set; } = string.Empty;
    }

    public static class SettingsStore
    {
        private static readonly string SettingsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FreeRdpWrapper",
            "settings.json"
        );

        public static AppSettings LoadSettings()
        {
            if (!File.Exists(SettingsFile))
                return new AppSettings();

            try
            {
                string json = File.ReadAllText(SettingsFile);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);

                if (settings != null && !string.IsNullOrEmpty(settings.MasterPasswordHash))
                {
                    // MasterPasswordHash is stored encrypted via DPAPI for extra security at rest.
                    try
                    {
                        settings.MasterPasswordHash = CryptoHelper.DecryptString(settings.MasterPasswordHash);
                    }
                    catch (CryptographicException)
                    {
                        // Could not decrypt DPAPI payload (maybe different user or machine)
                        settings.MasterPasswordHash = string.Empty;
                    }
                }

                return settings ?? new AppSettings();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
                return new AppSettings();
            }
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsFile);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Deep copy so we don't encrypt the active in-memory object
                var settingsToSave = new AppSettings
                {
                    MasterPasswordHash = !string.IsNullOrEmpty(settings.MasterPasswordHash)
                        ? CryptoHelper.EncryptString(settings.MasterPasswordHash)
                        : string.Empty
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(settingsToSave, options);
                File.WriteAllText(SettingsFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
