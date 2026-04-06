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
                
                // Decrypt passwords after loading
                foreach (var conn in connections)
                {
                    conn.Pass = CryptoHelper.DecryptString(conn.Pass);
                    conn.GatewayPass = CryptoHelper.DecryptString(conn.GatewayPass);
                }
                
                return connections;
            }
            catch
            {
                return new List<SavedConnection>();
            }
        }

        public void SaveConnections(List<SavedConnection> connections)
        {
            try
            {
                // Create a deep copy or encrypt in-place temporarily then decrypt back.
                // Best approach: clone the list to avoid modifying the in-memory models used by the UI.
                var connectionsToSave = new List<SavedConnection>();
                foreach (var conn in connections)
                {
                    // Serialize then deserialize to get a fresh clone
                    string tempJson = JsonSerializer.Serialize(conn);
                    var clone = JsonSerializer.Deserialize<SavedConnection>(tempJson);
                    if (clone != null)
                    {
                        clone.Pass = CryptoHelper.EncryptString(clone.Pass);
                        clone.GatewayPass = CryptoHelper.EncryptString(clone.GatewayPass);
                        connectionsToSave.Add(clone);
                    }
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(connectionsToSave, options);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving connections: {ex.Message}");
            }
        }
    }
}
