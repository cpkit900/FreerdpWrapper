using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FreeRdpWrapper.Models;

namespace FreeRdpWrapper.Services
{
    public class ScriptStore
    {
        private readonly string _filePath;

        public ScriptStore()
        {
            _filePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts.json");
        }

        public List<SavedScript> LoadScripts()
        {
            if (!File.Exists(_filePath))
                return new List<SavedScript>();

            try
            {
                string json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<SavedScript>>(json) ?? new List<SavedScript>();
            }
            catch
            {
                return new List<SavedScript>();
            }
        }

        public void SaveScripts(List<SavedScript> scripts)
        {
            try
            {
                string json = JsonSerializer.Serialize(scripts, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch { }
        }
    }
}
