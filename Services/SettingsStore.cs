using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
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

        // Window position & size
        public bool SaveWindowPosition { get; set; } = false;
        public int WindowX { get; set; } = -1;
        public int WindowY { get; set; } = -1;
        public int WindowWidth { get; set; } = -1;
        public int WindowHeight { get; set; } = -1;
        public FormWindowState WindowState { get; set; } = FormWindowState.Normal;

        // Workspace layout & tabs
        public bool SaveLayoutAndTabs { get; set; } = false;
        public int NavSplitterDistance { get; set; } = 280;
        public bool IsNavCollapsed { get; set; } = false;
        public int LogsSplitterDistance { get; set; } = 150;
        public bool IsLogsDrawerOpen { get; set; } = false;
        public List<string> OpenTabConnectionIds { get; set; } = new();
        public string ActiveTabName { get; set; } = string.Empty;

        // Movable toolbar order
        public List<string> ToolbarOrder { get; set; } = new() { "Standard", "Session", "Tools" };

        // Navigation Explorer Details (Header & Info Customization)
        public bool ShowHostInNavTree { get; set; } = false;
        public bool ShowUserInNavTree { get; set; } = false;
        public bool ShowPingStatusInNavTree { get; set; } = false;
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

                if (settings != null && (settings.ToolbarOrder == null || settings.ToolbarOrder.Count == 0))
                {
                    settings.ToolbarOrder = new List<string> { "Standard", "Session", "Tools" };
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

                // Deep copy using JSON serialize/deserialize so all properties are preserved
                string rawJson = JsonSerializer.Serialize(settings);
                var settingsToSave = JsonSerializer.Deserialize<AppSettings>(rawJson) ?? new AppSettings();

                if (!string.IsNullOrEmpty(settingsToSave.MasterPasswordHash))
                {
                    settingsToSave.MasterPasswordHash = VaultCrypto.EncryptSecret(settingsToSave.MasterPasswordHash);
                }

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
