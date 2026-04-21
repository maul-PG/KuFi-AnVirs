using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using KuFi.Engine.Sandbox;

namespace KuFi.UI.Views
{
    public partial class SandboxPage : Page
    {
        // Manager untuk memanipulasi Environment (Native Windows Token Hack)
        private SandboxEnvironment _environmentManager;
        
        // Monitor untuk mengambil statistik RAM/CPU secara asinkron tanpa freeze
        private Monitor _monitor;
        
        // Source data reaktif untuk ListView UI
        public ObservableCollection<AppItemViewModel> ActiveSandboxes { get; set; }

        public SandboxPage()
        {
            InitializeComponent();
            _environmentManager = new SandboxEnvironment();
            _monitor = new Monitor();
            
            // Subscribe ke pembaruan metrik dari Monitor.cs
            _monitor.OnUpdate += Monitor_OnUpdate;
            
            ActiveSandboxes = new ObservableCollection<AppItemViewModel>();
            AppsList.ItemsSource = ActiveSandboxes;
        }

        private void Monitor_OnUpdate(double cpuUsage, double ramUsageMb)
        {
            // Semua update elemen UI wajib dilakukan via Dispatcher (Main Thread)
            Dispatcher.Invoke(() =>
            {
                ProgCpu.Value = cpuUsage;
                TxtCpuVal.Text = $"{cpuUsage:F1} %";
                
                ProgRam.Value = ramUsageMb;
                TxtRamVal.Text = $"{ramUsageMb:F0} MB";
            });
        }

        #region Fitur Drag and Drop File

        // Menerima file yang di-drop ke seluruh area halaman atau area kotak
        private void Page_Drop(object sender, DragEventArgs e) => HandleDrop(e);
        private void DropArea_Drop(object sender, DragEventArgs e) => HandleDrop(e);

        private void DropArea_DragOver(object sender, DragEventArgs e)
        {
            // Indikasi visual bahwa area ini menerima file eksternal (Kursor "+")
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void HandleDrop(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string filePath = files[0];
                    if (filePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        LaunchFile(filePath);
                    }
                    else
                    {
                        MessageBox.Show("Sistem ini hanya dirancang untuk menjalankan eksekusi (.exe) biner dalam Sandbox.", "Tipe File Ditolak", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        #endregion

        #region Fitur Dialog Manual

        // Membuka File Explorer jika pengguna mengklik kotak besar alih-alih melempar file
        private void DropArea_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            BtnBrowse_Click(sender, e);
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe",
                Title = "Select Application to Isolate"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LaunchFile(openFileDialog.FileName);
            }
        }

        #endregion

        #region Logika Aplikasi Terisolasi

        /// <summary>
        /// Mengeksekusi file, mendaftarkannya ke UI, dan memicu monitoring sumber daya.
        /// </summary>
        private void LaunchFile(string filePath)
        {
            // Kirim ke Engine Sandbox untuk diberikan perlakuan Restricted Token
            var app = _environmentManager.LaunchSandboxed(filePath);
            
            if (app != null)
            {
                var vm = new AppItemViewModel
                {
                    ProcessId = app.ProcessId,
                    Name = app.Name,
                    Subtitle = $"PID: {app.ProcessId} • Admin Privileges Stripped",
                    TimeStr = app.LaunchTime.ToString("HH:mm:ss")
                };
                
                ActiveSandboxes.Add(vm);
                
                // Fokus monitoring diletakkan pada instance yang baru masuk
                _monitor.StopMonitoring();
                _monitor.StartMonitoring(app.ProcessId);
                
                UpdateInstancesCount();
            }
            else
            {
                MessageBox.Show("Aplikasi gagal dieksekusi secara aman. Pastikan file valid.", "Gagal Meluncurkan Sandbox", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Menerima klik pada tombol "STOP SANDBOX" yang ada di setiap baris aplikasi.
        /// </summary>
        private void StopApp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int processId)
            {
                // Engine mematikan proses
                _environmentManager.StopApp(processId);
                
                // Menghapus elemen dari tampilan
                var item = ActiveSandboxes.FirstOrDefault(a => a.ProcessId == processId);
                if (item != null)
                {
                    ActiveSandboxes.Remove(item);
                }

                // Logika perpindahan pantauan metrik RAM/CPU
                if (ActiveSandboxes.Count == 0)
                {
                    // Tidak ada aplikasi berjalan, nolkan indikator
                    _monitor.StopMonitoring();
                    ProgCpu.Value = 0; TxtCpuVal.Text = "0.0 %";
                    ProgRam.Value = 0; TxtRamVal.Text = "0 MB";
                }
                else
                {
                    // Pindahkan sorotan pengamatan ke aplikasi sisa pertama di daftar
                    _monitor.StopMonitoring();
                    _monitor.StartMonitoring(ActiveSandboxes[0].ProcessId);
                }
                
                UpdateInstancesCount();
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            // Sinkronisasi data di daftar UI dengan proses aktual Windows
            // Menghapus item dari ListBox jika ternyata aplikasi ditutup secara mandiri oleh user (lewat [X] di aplikasi itu sendiri).
            var activePids = _environmentManager.ActiveApps.Select(a => a.ProcessId).ToList();
            
            for (int i = ActiveSandboxes.Count - 1; i >= 0; i--)
            {
                if (!activePids.Contains(ActiveSandboxes[i].ProcessId))
                {
                    ActiveSandboxes.RemoveAt(i);
                }
            }
            
            UpdateInstancesCount();
        }

        private void UpdateInstancesCount()
        {
            TxtInstances.Text = ActiveSandboxes.Count.ToString("D2");
        }

        #endregion
    }

    /// <summary>
    /// Kelas Model Data khusus View (UI)
    /// </summary>
    public class AppItemViewModel
    {
        public int ProcessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string TimeStr { get; set; } = string.Empty;
    }
}
