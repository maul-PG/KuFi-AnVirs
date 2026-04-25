using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace KuFi.UI.ViewModels
{
    public class AppSettings
    {
        public bool RunAsAdmin { get; set; } = true;
        public bool RealTimeMonitor { get; set; } = true;
        public bool MinimizeToTray { get; set; } = true;
        public bool UseHeuristicEngine { get; set; } = true;
        public bool AutoQuarantine { get; set; } = true;
        public bool EnableWatchdog { get; set; } = true;
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
                if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFile, json);
            }
            catch { }
        }

        public static void ApplyStartupLogic()
        {
            try
            {
                string appName = "KuFi AnVirs";
                string? exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                
                if (string.IsNullOrEmpty(exePath)) return;

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (Current.RunAsAdmin) // Opsi ini juga merangkap izin Auto-Startup
                        {
                            key.SetValue(appName, $"\"{exePath}\"");
                        }
                        else
                        {
                            key.DeleteValue(appName, false);
                        }
                    }
                }
            }
            catch { }
        }
    }
}
