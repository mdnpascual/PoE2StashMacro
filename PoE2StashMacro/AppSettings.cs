using System;
using System.IO;
using System.Text.Json;

namespace PoE2StashMacro
{
    internal class AppSettings
    {
        public int SelectedMonitorIndex { get; set; } = -1;
        public string[] MonitorNames { get; set; } = Array.Empty<string>();
        public bool IsQuad { get; set; } = false;

        private const string SettingsFileName = "settings.json";

        public void LoadSettings()
        {
            if (File.Exists(SettingsFileName))
            {
                string json = File.ReadAllText(SettingsFileName);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    SelectedMonitorIndex = settings.SelectedMonitorIndex;
                    MonitorNames = settings.MonitorNames;
                    IsQuad = settings.IsQuad;
                }
            }
        }

        public void SaveSettings()
        {
            string json = JsonSerializer.Serialize(this);
            File.WriteAllText(SettingsFileName, json);
        }
    }
}
