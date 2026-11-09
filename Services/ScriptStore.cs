using System;
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
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FreeRdpWrapper");
            if (!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
            }
            _filePath = Path.Combine(appData, "scripts.json");

            // Migration: If legacy scripts.json exists in BaseDirectory, copy it over
            string legacyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts.json");
            if (File.Exists(legacyPath) && !File.Exists(_filePath))
            {
                try { File.Copy(legacyPath, _filePath); } catch { }
            }
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
