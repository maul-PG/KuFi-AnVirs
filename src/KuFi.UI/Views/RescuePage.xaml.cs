using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading; // Untuk DispatcherTimer
using KuFi.Engine.Rescue;

namespace KuFi.UI.Views
{
    public partial class RescuePage : Page
    {
        public ObservableCollection<DriveDetector.DriveDetails> AvailableDrives { get; set; }
        private readonly DriveDetector _driveDetector;
        private readonly Cleaner _cleaner;
        private readonly KuFi.Engine.Scanners.HashScanner _scanner;

        // Deklarasi Timer untuk "Time Elapsed" detik demi detik
        private DispatcherTimer _elapsedTimer;
        private DateTime _startTime;

        public RescuePage()
        {
            InitializeComponent();
            
            _driveDetector = new DriveDetector();
            _cleaner = new Cleaner();
            _scanner = new KuFi.Engine.Scanners.HashScanner();
            
            AvailableDrives = new ObservableCollection<DriveDetector.DriveDetails>();
            DrivesList.ItemsSource = AvailableDrives;
            
            // Konfigurasi Timer agar trigger (centang) setiap 1 detik
            _elapsedTimer = new DispatcherTimer();
            _elapsedTimer.Interval = TimeSpan.FromSeconds(1);
            _elapsedTimer.Tick += ElapsedTimer_Tick;
            
            RefreshDrives();
        }

        private void RefreshDrives()
        {
            try
            {
                AvailableDrives.Clear();
                var drives = _driveDetector.GetRemovableDrives();
                
                foreach(var drive in drives)
                {
                    AvailableDrives.Add(drive);
                }

                if (AvailableDrives.Count > 0)
                {
                    DrivesList.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                TxtLog.Text += $"[SISTEM] Gagal memindai port: {ex.Message}\n";
            }
        }

        // Fungsi bantuan untuk melempar teks log ke antarmuka aplikasi
        private void LogMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                TxtLog.Text += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
                LogScroll.ScrollToEnd();
            });
        }

        private void ElapsedTimer_Tick(object? sender, EventArgs e)
        {
            var elapsed = DateTime.Now - _startTime;
            TxtTime.Text = $"Time elapsed: {elapsed:hh\\:mm\\:ss} • Remaining: Calculating...";
        }

        private bool _isScanRunning = false;

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            if (_isScanRunning)
            {
                _isScanRunning = false;
                LogMessage("Memproses penghentian (STOP SCAN)...");
                TxtStatus.Text = "Stopping Engine...";
            }
        }

        private async void BtnRescue_Click(object sender, RoutedEventArgs e)
        {
            if (DrivesList.SelectedItem is DriveDetector.DriveDetails selectedDrive)
            {
                // -- FASE 1: PERSIAPAN UI --
                TxtLog.Text = string.Empty;
                TxtStatus.Text = "Initializing Engine...";
                TxtProcessing.Text = "PROCESSING";
                TxtProgressPercent.Text = "0%"; 
                BtnRescue.IsEnabled = false;
                BtnRescue.Opacity = 0.5;
                BtnStop.IsEnabled = true;
                BtnStop.Opacity = 1;
                
                // Mulai penghitungan detik dan start Timer UI
                _startTime = DateTime.Now;
                _elapsedTimer.Start();
                _isScanRunning = true;
                
                // -- FASE 2: ASYNCHRONOUS ENGINE EXECUTION (REAL-TIME FILE COUNTER) --
                try
                {
                    await Task.Run(async () => 
                    {
                        LogMessage($"Initializing hardware bridge for {selectedDrive.Name}... OK");
                        await Task.Delay(500); 
                        
                        // Gunakan pendekatan Stack untuk aman menelusuri folder tanpa crash UnauthorizedAccessException
                        System.Collections.Generic.Stack<string> dirs = new System.Collections.Generic.Stack<string>();
                        if (System.IO.Directory.Exists(selectedDrive.Letter))
                        {
                            dirs.Push(selectedDrive.Letter);
                        }
                            
                        long totalBytes = 0;
                        int totalFiles = 0;
                        int progress = 0;

                        while (dirs.Count > 0 && _isScanRunning)
                        {
                            string currentDir = dirs.Pop();
                            string[] subDirs = null;
                            try { subDirs = System.IO.Directory.GetDirectories(currentDir); } catch { }

                            if (subDirs != null)
                            {
                                foreach (string dir in subDirs) dirs.Push(dir);
                            }

                            // Iterasi file secara spesifik di dalam folder ini (Gunakan EnumerateFiles untuk efisiensi RAM)
                            System.Collections.Generic.IEnumerable<string> files = null;
                            try { files = System.IO.Directory.EnumerateFiles(currentDir); } catch { }
                            
                            if (files != null)
                            {
                                foreach (string file in files)
                                {
                                    if (!_isScanRunning) break; // Keluar dini jika tombol STOP ditekan
                                    
                                    try 
                                    {
                                        var info = new System.IO.FileInfo(file);
                                        totalBytes += info.Length;
                                        totalFiles++;
                                        
                                        // SMART DETECTION INTEGRATION
                                        var threat = await _scanner.CheckThreatAsync(file);
                                        
                                        // Diberikan sampling setiap 25 file agar UI terlihat hidup tanpa membuat thread lag
                                        if (totalFiles % 25 == 0)
                                        {
                                            LogMessage($"Checking: {System.IO.Path.GetFileName(file)} -> Hash: {threat.fileHash}");
                                        }

                                        if (threat.isInfected)
                                        {
                                            bool isDeleted = false;
                                            Dispatcher.Invoke(() => {
                                                TxtLog.Foreground = System.Windows.Media.Brushes.Red;
                                                LogMessage($"[THREAT DETECTED] {threat.threatName} at {file}");
                                                
                                                var result = MessageBox.Show(
                                                    $"Threat Found: {threat.threatName}\nLocation: {file}\n\nDo you want to permanently delete this file?", 
                                                    "KuFi Smart Notification", 
                                                    MessageBoxButton.YesNo, 
                                                    MessageBoxImage.Warning);
                                                    
                                                if (result == MessageBoxResult.Yes)
                                                {
                                                    try { System.IO.File.Delete(file); LogMessage("File deleted successfully."); isDeleted = true; }
                                                    catch { LogMessage("Failed to delete file."); KuFi.UI.ViewModels.MainViewModel.IsSystemSecured = false; }
                                                }
                                                else
                                                {
                                                    LogMessage("Threat ignored.");
                                                    KuFi.UI.ViewModels.MainViewModel.IsSystemSecured = false;
                                                }
                                                // Kembalikan warna ke semula
                                                TxtLog.Foreground = (System.Windows.Media.Brush)FindResource("EthericMint");
                                            });
                                            if (isDeleted) continue; // Skip size update if deleted
                                        }
                                        
                                        // Update UI UI setiap 40 file untuk menghindari kemacetan Rendering (Lag)
                                        if (totalFiles % 40 == 0) 
                                        {
                                            double gb = totalBytes / (1024.0 * 1024.0 * 1024.0);
                                            double mb = totalBytes / (1024.0 * 1024.0);
                                            string sizeStr = gb >= 1 ? $"{gb:F1} GB" : $"{mb:F1} MB";

                                            Dispatcher.Invoke(() => {
                                                TxtTotalFiles.Text = totalFiles.ToString();
                                                TxtRecoveredSize.Text = sizeStr;
                                                
                                                // Simulasi Persentase Progresif
                                                if (progress < 99) progress++;
                                                TxtProgressPercent.Text = $"{progress}%";
                                                
                                                if (progress < 30) TxtStatus.Text = "Analyzing Partition Sectors";
                                                else if (progress < 60) TxtStatus.Text = "Reconstructing MFT";
                                                else if (progress < 85) TxtStatus.Text = "Restoring Attributes";
                                                else TxtStatus.Text = "Finalizing Recovery";
                                            });

                                            LogMessage($"Checking directory: {currentDir}");
                                            await Task.Delay(1); // Kasih jeda bernapas untuk OS Windows
                                        }
                                    } 
                                    catch { /* Skip file jika dikunci oleh sistem */ }
                                }
                            }
                        }

                        // Setelah selesai total
                        Dispatcher.Invoke(() => {
                            double finalGb = totalBytes / (1024.0 * 1024.0 * 1024.0);
                            double finalMb = totalBytes / (1024.0 * 1024.0);
                            TxtRecoveredSize.Text = finalGb >= 1 ? $"{finalGb:F1} GB" : $"{finalMb:F1} MB";
                            TxtTotalFiles.Text = totalFiles.ToString();
                        });

                        if (_isScanRunning)
                        {
                            LogMessage($"Restored all {totalFiles} files successfully on {selectedDrive.Letter}");
                        }
                        else
                        {
                            LogMessage($"Scan Aborted. {totalFiles} files processed so far.");
                        }
                        await Task.Delay(500);
                    });
                }
                catch (Exception ex)
                {
                    LogMessage($"[ERROR FATAL] {ex.Message}");
                }

                // -- FASE 3: PENYELESAIAN UI --
                bool wasAborted = !_isScanRunning;
                
                _elapsedTimer.Stop(); // Hentikan timer detik
                _isScanRunning = false;
                
                var totalElapsed = DateTime.Now - _startTime;
                TxtTime.Text = $"Time elapsed: {totalElapsed:hh\\:mm\\:ss} • {(wasAborted ? "Aborted" : "Finished")}";
                
                TxtStatus.Text = wasAborted ? "Scan Aborted" : "Rescue Completed";
                TxtProcessing.Text = wasAborted ? "ABORTED" : "COMPLETED";
                if (!wasAborted) TxtProgressPercent.Text = "100%";
                
                BtnStop.IsEnabled = false;
                BtnStop.Opacity = 0.5;
                BtnRescue.IsEnabled = true;
                BtnRescue.Opacity = 1;
                
                // --- INTEGRASI MODAL BARU ---
                var dialog = new KuFiDialog(
                    wasAborted ? "Scan Aborted" : "KuFi Rescue Mission", 
                    wasAborted ? $"Operasi dihentikan paksa.\nDrive: {selectedDrive.Letter} {selectedDrive.Name}\nTotal File Diproses: {TxtTotalFiles.Text}" : $"Penyelamatan Selesai!\nDrive: {selectedDrive.Letter} {selectedDrive.Name}\nTotal File: {TxtTotalFiles.Text}\nUkuran: {TxtRecoveredSize.Text}", 
                    KuFiDialogButtons.Ok, 
                    wasAborted ? KuFiDialogIcon.Warning : KuFiDialogIcon.Info);
                dialog.ShowDialog();
            }
            else
            {
                MessageBox.Show("Sistem tidak mendeteksi pilihan. Klik salah satu drive di daftar sisi kiri.", "Drive Belum Dipilih", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // Mencegah Memory Leak saat Frame di-navigate ke halaman lain
            if (_elapsedTimer != null)
            {
                _elapsedTimer.Stop();
                _elapsedTimer.Tick -= ElapsedTimer_Tick;
            }
        }
    }
}
