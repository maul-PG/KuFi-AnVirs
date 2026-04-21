using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace KuFi.UI.Views
{
    public partial class DashboardPage : Page
    {
        private PerformanceCounter? _cpuCounter;
        private PerformanceCounter? _ramCounter;
        private DispatcherTimer _timer;

        public DashboardPage()
        {
            InitializeComponent();
            
            // 1. Mengubah Identitas berdasarkan Environment OS
            TxtWelcome.Text = $"Welcome, {Environment.MachineName}!";

            // 2. Menggunakan System.Diagnostics.PerformanceCounter sesuai SRS
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            }
            catch
            {
                // Mencegah force close jika Windows tidak mengizinkan akses counter
            }

            // Inisialisasi Timer dengan interval 1 detik untuk membatasi konsumsi RAM rendering
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (_cpuCounter != null && _ramCounter != null)
                {
                    // Ambil nilai dari OS
                    float cpu = _cpuCounter.NextValue();
                    float ramMb = _ramCounter.NextValue();
                    double ramGb = ramMb / 1024.0;

                    // Update UI Real-time
                    TxtCpu.Text = $"{cpu:F1}%";
                    TxtRam.Text = $"{ramGb:F1} GB";
                }
            }
            catch (Exception)
            {
                TxtCpu.Text = "N/A";
                TxtRam.Text = "N/A";
            }

            // Sinkronisasi status keamanan sistem dari Global State
            if (!KuFi.UI.ViewModels.MainViewModel.IsSystemSecured)
            {
                TxtSystemStatus.Text = "KuFi AnVirs • ACTION REQUIRED";
                TxtSystemStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
            else
            {
                TxtSystemStatus.Text = "KuFi AnVirs • SYSTEM SECURED";
                TxtSystemStatus.Foreground = (System.Windows.Media.Brush)FindResource("EthericMint");
            }
        }

        private bool _isScanning = false;

        private void Page_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                RunQuickScanAsync(files);
            }
        }

        private void ScanNow_Click(object sender, RoutedEventArgs e)
        {
            if (_isScanning) return;
            
            // Lokasi default One-Tap Quick Scan
            string[] defaultPaths = new string[] {
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
            };
            
            RunQuickScanAsync(defaultPaths);
        }

        private async void RunQuickScanAsync(string[] targetPaths)
        {
            if (_isScanning) return;
            _isScanning = true;
            
            QuickScanProgress.Visibility = Visibility.Visible;
            TxtQuickScanDetail.Visibility = Visibility.Visible;
            TxtScanStatus.Text = "S C A N N I N G . . .";
            TxtScanStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(250, 204, 21));
            
            int[] stats = new int[2]; // index 0: Scanned, index 1: Threats
            var scanner = new KuFi.Engine.Scanners.HashScanner();

            try
            {
                await Task.Run(async () =>
                {
                    System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.BelowNormal;

                    foreach (string path in targetPaths)
                    {
                        if (!System.IO.Directory.Exists(path) && !System.IO.File.Exists(path)) continue;

                        System.Collections.Generic.Stack<string> dirs = new System.Collections.Generic.Stack<string>();
                        
                        if (System.IO.Directory.Exists(path))
                            dirs.Push(path);
                        else 
                        {
                            // Jika objek yang didrop adalah sebuah file langsung
                            await ProcessSingleFileAsync(path, scanner, stats);
                            continue;
                        }

                        while (dirs.Count > 0)
                        {
                            string currentDir = dirs.Pop();
                            
                            string[] subDirs = null;
                            try { subDirs = System.IO.Directory.GetDirectories(currentDir); } catch { }
                            if (subDirs != null) { foreach (var d in subDirs) dirs.Push(d); }

                            string[] files = null;
                            try { files = System.IO.Directory.GetFiles(currentDir); } catch { }
                            
                            if (files != null)
                            {
                                foreach (var file in files)
                                {
                                    await ProcessSingleFileAsync(file, scanner, stats);
                                }
                            }
                        }
                    }
                });

                // Selesai Scanning, tampilkan rekapitulasi
                Dispatcher.Invoke(() =>
                {
                    TxtScanStatus.Text = "S C A N   N O W";
                    TxtScanStatus.Foreground = (System.Windows.Media.Brush)FindResource("EthericMint");
                    QuickScanProgress.Visibility = Visibility.Hidden;
                    TxtQuickScanDetail.Visibility = Visibility.Hidden;
                });

                MessageBox.Show($"Quick Scan Complete!\nTotal Files Scanned: {stats[0]}\nThreats Found: {stats[1]}", "KuFi Quick Scan", MessageBoxButton.OK, stats[1] > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    TxtScanStatus.Text = "S C A N   N O W";
                    TxtScanStatus.Foreground = (System.Windows.Media.Brush)FindResource("EthericMint");
                    QuickScanProgress.Visibility = Visibility.Hidden;
                    TxtQuickScanDetail.Visibility = Visibility.Hidden;
                });
                MessageBox.Show($"Terjadi kesalahan sistem: {ex.Message}", "Error Scanner", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isScanning = false;
            }
        }

        private async Task ProcessSingleFileAsync(string file, KuFi.Engine.Scanners.HashScanner scanner, int[] stats)
        {
            var threat = await scanner.CheckThreatAsync(file);

            Dispatcher.Invoke(() => {
                TxtQuickScanDetail.Text = $"Checking: {System.IO.Path.GetFileName(file)} -> Hash: {threat.fileHash}";
            });

            if (threat.isInfected)
            {
                stats[1]++;
                Dispatcher.Invoke(() =>
                {
                    var result = MessageBox.Show(
                        $"THREAT DETECTED: {threat.threatName}\nLocation: {file}\n\nDo you want to permanently delete this file? (Yes = Delete, No = Ignore)", 
                        "KuFi Action Required", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Error);
                        
                    if (result == MessageBoxResult.Yes)
                    {
                        try { System.IO.File.Delete(file); } catch { KuFi.UI.ViewModels.MainViewModel.IsSystemSecured = false; }
                    }
                    else
                    {
                        KuFi.UI.ViewModels.MainViewModel.IsSystemSecured = false;
                    }
                });
            }
            stats[0]++;
            await Task.Delay(2); // Kasih jeda kecil anti-hang pada OS Windows dan RAM 4GB
        }

        private void LaunchRescue_Click(object sender, RoutedEventArgs e)
        {
            // Fix navigasi: Panggil MainWindow untuk menyinkronkan RadioButton UI
            // sekaligus memuat halaman melalui MainViewModel agar tidak ada konflik history ("bingung").
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.NavRescue.IsChecked = true;
                if (mainWindow.DataContext is KuFi.UI.ViewModels.MainViewModel vm)
                {
                    vm.CurrentPage = new Uri("Views/RescuePage.xaml", UriKind.Relative);
                }
            }
        }

        private void LaunchSandbox_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.NavSandbox.IsChecked = true;
                if (mainWindow.DataContext is KuFi.UI.ViewModels.MainViewModel vm)
                {
                    vm.CurrentPage = new Uri("Views/SandboxPage.xaml", UriKind.Relative);
                }
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // PENCEGAHAN FORCE CLOSE & MEMORY LEAK:
            // Pastikan timer dihentikan dan resources PerformanceCounter di-dispose 
            // setiap kali pengguna berpindah halaman untuk memastikan RAM stabil.
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
            }
            
            _cpuCounter?.Dispose();
            _ramCounter?.Dispose();
        }
    }
}
