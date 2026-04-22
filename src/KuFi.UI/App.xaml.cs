using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using KuFi.Data;
using KuFi.UI.Views;

namespace KuFi.UI
{
    public partial class App : Application
    {
        private FileSystemWatcher? _watcherDownloads;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> _recentlyScanned = new();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var dbContext = new SQLiteContext();
                
                using (var conn = dbContext.GetConnection())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "INSERT OR IGNORE INTO threat_library (Hash, Name, Type, Severity) VALUES ('e99a18c428cb38d5f260853678922e03', 'EICAR-Test-File', 'Test', 1)";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"[Sistem KuFi] Gagal menginisialisasi database.\n\n{ex.Message}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }

            // REAL-TIME FILE WATCHER (HANYA FOKUS DOWNLOADS USER AKTIF)
            try
            {
                string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (Directory.Exists(downloadsPath))
                {
                    _watcherDownloads = new FileSystemWatcher(downloadsPath);
                    _watcherDownloads.IncludeSubdirectories = true;
                    _watcherDownloads.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
                    _watcherDownloads.Created += OnFileDownloaded;
                    _watcherDownloads.Changed += OnFileDownloaded;
                    _watcherDownloads.EnableRaisingEvents = true;
                    Console.WriteLine("DEBUG: Real-time Guard is watching: " + downloadsPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DEBUG: Real-time Guard Error - " + ex.Message);
            }
        }

        private async void OnFileDownloaded(object sender, FileSystemEventArgs e)
        {
            // Debounce
            if (_recentlyScanned.TryGetValue(e.FullPath, out var lastScan) && (DateTime.Now - lastScan).TotalSeconds < 5)
                return;
            _recentlyScanned[e.FullPath] = DateTime.Now;

            await Task.Delay(2500);

            _ = Task.Run(async () =>
            {
                System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Lowest;
                try
                {
                    var scanner = new KuFi.Engine.Scanners.HashScanner();
                    var threat = await scanner.CheckThreatAsync(e.FullPath);

                    if (threat.isInfected)
                    {
                        // WAJIB gunakan Dispatcher.Invoke untuk sync UI
                        Application.Current.Dispatcher.Invoke(() => 
                        {
                            KuFi.UI.ViewModels.MainViewModel.IsSystemSecured = false;
                            
                            // Tambahkan ke Log Activity
                            KuFi.UI.ViewModels.MainViewModel.ActivityLogs.Insert(0, new KuFi.UI.ViewModels.LogEntry {
                                Timestamp = DateTime.Now.ToString("HH:mm:ss"),
                                Event = "Ancaman Terdeteksi",
                                Path = e.FullPath,
                                Action = "Menunggu Tindakan"
                            });

                            // Tampilkan Custom Notification Window
                            var notifWindow = new NotificationWindow(e.Name, e.FullPath);
                            notifWindow.Show();
                        });
                    }
                }
                catch { }
            });
        }
    }
}
