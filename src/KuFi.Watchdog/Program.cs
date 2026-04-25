using System;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Linq;
using System.Security.Principal;

namespace KuFi.Watchdog
{
    class Program
    {
        static void Main(string[] args)
        {
            string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KuFi", "watchdog.log");
            try { Directory.CreateDirectory(Path.GetDirectoryName(logPath)); File.WriteAllText(logPath, $"[{DateTime.Now}] Watchdog Started.\n"); } catch { }

            // Robust initialization delay to prevent race conditions during app startup/restart
            Thread.Sleep(3000);

            string mainAppPath = "";

            // Priority 1: Argument passed from WatchdogService
            if (args.Length > 0 && !string.IsNullOrEmpty(args[0])) 
            {
                mainAppPath = args[0];
            }
            // Priority 2: Relative path assuming same directory
            else 
            {
                mainAppPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KuFi AnVirs.exe");
                // Secondary guess if named differently
                if (!File.Exists(mainAppPath))
                    mainAppPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KuFi.UI.exe");
            }

            // Production-grade watchdog loop
            while (true)
            {
                try
                {
                    // Detect UI process (exclude the extension)
                    var uiProcesses = Process.GetProcessesByName("KuFi AnVirs")
                        .Concat(Process.GetProcessesByName("KuFi.UI"))
                        .ToList();

                    if (uiProcesses.Count == 0)
                    {
                        if (File.Exists(mainAppPath))
                        {
                            RestartMainApp(mainAppPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    try { File.AppendAllText(logPath, $"[{DateTime.Now}] Loop Error: {ex.Message}\n"); } catch { }
                    // Silently log or handle access denied errors common in security software
                    Debug.WriteLine($"Watchdog Error: {ex.Message}");
                }

                try { File.AppendAllText(logPath, $"[{DateTime.Now}] Heartbeat pulse...\n"); } catch { }
                // 2-second heartbeat as requested
                Thread.Sleep(2000); 
            }
        }

        private static void RestartMainApp(string path)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = path,
                    WorkingDirectory = Path.GetDirectoryName(path), // CRITICAL: Fixes DLL loading issues
                    UseShellExecute = true,
                    // Attempt to maintain elevation if the watchdog itself is elevated
                    Verb = IsAdministrator() ? "runas" : "" 
                };

                Process.Start(startInfo);
            }
            catch { /* Handle potential UAC cancellation or file locks */ }
        }

        private static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }
}