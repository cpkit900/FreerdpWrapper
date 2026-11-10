using System;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using FreeRdpWrapper.Models;

namespace FreeRdpWrapper.Services
{
    public enum LogLevelSetting
    {
        Off,
        Error,
        Info,
        Trace
    }

    public class AppSettings
    {
        public string MasterPasswordHash { get; set; } = string.Empty;
        public LogLevelSetting LogLevel { get; set; } = LogLevelSetting.Info;
        public bool AutoShowLogsOnError { get; set; } = true;
        public bool ShowQuickConnectBar { get; set; } = true;
        public bool ShowStandardToolbar { get; set; } = true;
        public bool ShowSessionToolbar { get; set; } = true;
        public bool ShowToolsToolbar { get; set; } = true;
        public bool ShowStatusBar { get; set; } = true;
        public AppTheme Theme { get; set; } = AppTheme.HighContrast;
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
                    try
                    {
                        settings.MasterPasswordHash = VaultCrypto.DecryptSecret(settings.MasterPasswordHash);
                    }
                    catch (CryptographicException)
                    {
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

                var settingsToSave = new AppSettings
                {
                    MasterPasswordHash = !string.IsNullOrEmpty(settings.MasterPasswordHash)
                        ? VaultCrypto.EncryptSecret(settings.MasterPasswordHash)
                        : string.Empty,
                    LogLevel = settings.LogLevel,
                    AutoShowLogsOnError = settings.AutoShowLogsOnError,
                    ShowQuickConnectBar = settings.ShowQuickConnectBar,
                    ShowStandardToolbar = settings.ShowStandardToolbar,
                    ShowSessionToolbar = settings.ShowSessionToolbar,
                    ShowToolsToolbar = settings.ShowToolsToolbar,
                    ShowStatusBar = settings.ShowStatusBar,
                    Theme = settings.Theme
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
