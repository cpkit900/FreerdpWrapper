using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FreeRdpWrapper.Models;

namespace FreeRdpWrapper.Services
{
    public class ConnectionStore
    {
        private readonly string _filePath;

        public ConnectionStore()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FreeRdpWrapper");
            if (!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
            }
            _filePath = Path.Combine(appData, "connections.json");
        }

        public List<SavedConnection> LoadConnections()
        {
            if (!File.Exists(_filePath))
            {
                return new List<SavedConnection>();
            }

            try
            {
                string json = File.ReadAllText(_filePath);
                var connections = JsonSerializer.Deserialize<List<SavedConnection>>(json) ?? new List<SavedConnection>();
                
                // Decrypt passwords securely using VaultCrypto (supports v2 AES-GCM and legacy v1 DPAPI)
                foreach (var conn in connections)
                {
                    conn.Pass = VaultCrypto.DecryptSecret(conn.Pass);
                    conn.GatewayPass = VaultCrypto.DecryptSecret(conn.GatewayPass);
                }
                
                return connections;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConnectionStore] Error loading connections: {ex.Message}");
                return new List<SavedConnection>();
            }
        }

        public void SaveConnections(List<SavedConnection> connections)
        {
            try
            {
                // Clone the list to encrypt for disk storage without mutating live in-memory objects
                var connectionsToSave = new List<SavedConnection>();
                foreach (var conn in connections)
                {
                    string tempJson = JsonSerializer.Serialize(conn);
                    var clone = JsonSerializer.Deserialize<SavedConnection>(tempJson);
                    if (clone != null)
                    {
                        clone.Pass = VaultCrypto.EncryptSecret(clone.Pass);
                        clone.GatewayPass = VaultCrypto.EncryptSecret(clone.GatewayPass);
                        connectionsToSave.Add(clone);
                    }
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(connectionsToSave, options);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConnectionStore] Error saving connections: {ex.Message}");
            }
        }

        /// <summary>
        /// Exports all connections into an encrypted .frdpvault archive protected with a user password.
        /// </summary>
        public void ExportVault(string targetPath, string passphrase, List<SavedConnection> connections)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string plainJson = JsonSerializer.Serialize(connections, options);
            byte[] vaultBytes = VaultCrypto.ExportEncryptedVault(plainJson, passphrase);
            File.WriteAllBytes(targetPath, vaultBytes);
        }

        /// <summary>
        /// Imports connections from an encrypted .frdpvault archive using the passphrase.
        /// </summary>
        public List<SavedConnection> ImportVault(string sourcePath, string passphrase)
        {
            byte[] vaultBytes = File.ReadAllBytes(sourcePath);
            string plainJson = VaultCrypto.ImportEncryptedVault(vaultBytes, passphrase);
            var imported = JsonSerializer.Deserialize<List<SavedConnection>>(plainJson) ?? new List<SavedConnection>();
            return imported;
        }

        /// <summary>
        /// Imports a connection from a standard Microsoft .rdp file.
        /// </summary>
        public SavedConnection ImportRdp(string rdpFilePath)
        {
            return RdpFileParser.Parse(rdpFilePath);
        }

        /// <summary>
        /// Exports a connection to a standard Microsoft .rdp file.
        /// </summary>
        public void ExportRdp(SavedConnection conn, string rdpFilePath)
        {
            string content = RdpFileParser.ExportToRdpString(conn);
            File.WriteAllText(rdpFilePath, content);
        }
    }
}
