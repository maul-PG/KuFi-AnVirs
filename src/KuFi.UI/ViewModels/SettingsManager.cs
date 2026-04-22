using System.IO;
using System.Text.Json;

namespace KuFi.UI.ViewModels
{
    public class AppSettings
    {
        public bool RunAsAdmin { get; set; } = true;
        public bool RealTimeMonitor { get; set; } = true;
        public bool MinimizeToTray { get; set; } = true;
    }

    public static class SettingsManager
    {
        private static string _settingsFile = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "KuFi", "settings.json");

        public static AppSettings Current { get; set; } = new AppSettings();

        public static void Load()
        {
            try
            {
                if (File.Exists(_settingsFile))
                {
                    string json = File.ReadAllText(_settingsFile);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null) Current = settings;
                }
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(_settingsFile);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFile, json);
            }
            catch { }
        }
    }
}
