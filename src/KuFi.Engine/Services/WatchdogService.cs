using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace KuFi.Engine.Services
{
    public static class WatchdogService
    {
        private static Timer? _healingTimer;
        private const string WATCHDOG_NAME = "KuFi.Watchdog";
        
        /// <summary>
        /// Controls whether the self-healing logic should be active.
        /// Decoupled from SettingsManager to avoid circular dependencies.
        /// </summary>
        public static bool IsEnabled { get; set; } = true;

        public static void StartWatchdog()
        {
            string serviceLog = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KuFi", "service.log");
            try { File.AppendAllText(serviceLog, $"[{DateTime.Now}] StartWatchdog() called.\n"); } catch { }
            try
            {
                string watchdogExe = WATCHDOG_NAME + ".exe";
                string watchdogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, watchdogExe);
                try { File.AppendAllText(serviceLog, $"[{DateTime.Now}] Checking path: {watchdogPath}\n"); } catch { }

                if (File.Exists(watchdogPath))
                {
                    try { File.AppendAllText(serviceLog, $"[{DateTime.Now}] Watchdog file found.\n"); } catch { }
                    // Correct process detection (name only, no extension)
                    var processes = Process.GetProcessesByName(WATCHDOG_NAME);
                    try { File.AppendAllText(serviceLog, $"[{DateTime.Now}] Active Watchdogs: {processes.Length}\n"); } catch { }
                    
                    if (processes.Length == 0)
                    {
                        var currentProcess = Process.GetCurrentProcess();
                        string? mainExePath = currentProcess.MainModule?.FileName;
                        
                        if (string.IsNullOrEmpty(mainExePath)) return;
                        try { File.AppendAllText(serviceLog, $"[{DateTime.Now}] Starting Watchdog process...\n"); } catch { }

                        Process.Start(new ProcessStartInfo
                        {
                            FileName = watchdogPath,
                            Arguments = $"\"{mainExePath}\"",
                            CreateNoWindow = true,
                            UseShellExecute = true, // Required for Hidden WindowStyle and Elevation
                            WindowStyle = ProcessWindowStyle.Hidden,
                            WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                        });
                    }
                }
                else
                {
                    try { File.AppendAllText(serviceLog, $"[{DateTime.Now}] ERROR: Watchdog file NOT FOUND at {watchdogPath}\n"); } catch { }
                }

                // Initialize Self-Healing Monitor (Mutual Protection)
                if (_healingTimer == null)
                {
                    _healingTimer = new Timer(HealingCallback, null, 5000, 5000); // Check every 5 seconds
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Watchdog Service Error: {ex.Message}");
            }
        }

        private static void HealingCallback(object? state)
        {
            try 
            {
                // Antigravity Mutual Protection Logic
                if (IsEnabled)
                {
                    var processes = Process.GetProcessesByName(WATCHDOG_NAME);
                    if (processes.Length == 0)
                    {
                        StartWatchdog(); 
                    }
                }
            }
            catch { }
        }

        public static void StopWatchdog()
        {
            try
            {
                // Stop the healing timer first to prevent auto-restart
                _healingTimer?.Dispose();
                _healingTimer = null;

                var processes = Process.GetProcessesByName(WATCHDOG_NAME);
                foreach (var process in processes)
                {
                    try { process.Kill(); } catch { }
                }
            }
            catch { }
        }
    }
}
