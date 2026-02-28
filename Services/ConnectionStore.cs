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
                return JsonSerializer.Deserialize<List<SavedConnection>>(json) ?? new List<SavedConnection>();
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
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(connections, options);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving connections: {ex.Message}");
            }
        }
    }
}
