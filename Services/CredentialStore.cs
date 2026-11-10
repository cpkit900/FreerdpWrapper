using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FreeRdpWrapper.Models;

namespace FreeRdpWrapper.Services
{
    public class CredentialStore
    {
        private readonly string _filePath;

        public CredentialStore()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FreeRdpWrapper");
            if (!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
            }
            _filePath = Path.Combine(appData, "credentials.json");

            // Migration: Check legacy base directory
            string legacyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "credentials.json");
            if (File.Exists(legacyPath) && !File.Exists(_filePath))
            {
                try { File.Copy(legacyPath, _filePath); } catch { }
            }
        }

        public List<SavedCredential> LoadCredentials(IEnumerable<SavedConnection>? fallbackConnections = null)
        {
            if (!File.Exists(_filePath))
            {
                // Seed initial credentials from existing connections if available
                var seeded = new List<SavedCredential>();
                if (fallbackConnections != null)
                {
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var c in fallbackConnections)
                    {
                        if (!string.IsNullOrWhiteSpace(c.User) || !string.IsNullOrWhiteSpace(c.Pass))
                        {
                            string key = $"{c.Domain}\\{c.User}".TrimStart('\\');
                            if (!string.IsNullOrEmpty(key) && seen.Add(key))
                            {
                                seeded.Add(new SavedCredential
                                {
                                    Title = string.IsNullOrWhiteSpace(c.Name) ? key : $"{c.Name} Account",
                                    Username = c.User,
                                    Password = c.Pass,
                                    Domain = c.Domain,
                                    Description = $"Imported from {c.Host}"
                                });
                            }
                        }
                    }
                    if (seeded.Count > 0)
                    {
                        SaveCredentials(seeded);
                    }
                }
                return seeded;
            }

            try
            {
                string json = File.ReadAllText(_filePath);
                var credentials = JsonSerializer.Deserialize<List<SavedCredential>>(json) ?? new List<SavedCredential>();

                foreach (var cred in credentials)
                {
                    cred.Password = VaultCrypto.DecryptSecret(cred.Password);
                }

                return credentials;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CredentialStore] Error loading credentials: {ex.Message}");
                return new List<SavedCredential>();
            }
        }

        public void SaveCredentials(List<SavedCredential> credentials)
        {
            try
            {
                var credentialsToSave = new List<SavedCredential>();
                foreach (var cred in credentials)
                {
                    credentialsToSave.Add(new SavedCredential
                    {
                        Id = cred.Id,
                        Title = cred.Title,
                        Username = cred.Username,
                        Password = VaultCrypto.EncryptSecret(cred.Password),
                        Domain = cred.Domain,
                        Description = cred.Description
                    });
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(credentialsToSave, options);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CredentialStore] Error saving credentials: {ex.Message}");
            }
        }
    }
}

