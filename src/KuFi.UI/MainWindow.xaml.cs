using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms; // Butuh UseWindowsForms = true di .csproj
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace KuFi.UI
{
    public partial class MainWindow : Window
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            int useImmersiveDarkMode = 1;
            DwmSetWindowAttribute(hwnd, 20, ref useImmersiveDarkMode, sizeof(int));
            DwmSetWindowAttribute(hwnd, 19, ref useImmersiveDarkMode, sizeof(int));
        }

        private NotifyIcon _notifyIcon;
        public static bool AutoStartScan = false;

        public MainWindow()
        {
            KuFi.UI.ViewModels.SettingsManager.Load();
            KuFi.UI.ViewModels.SettingsManager.ApplyStartupLogic();

            if (KuFi.UI.ViewModels.SettingsManager.Current.EnableWatchdog)
            {
                KuFi.Engine.Services.WatchdogService.IsEnabled = true;
                KuFi.Engine.Services.WatchdogService.StartWatchdog();
            }
            else
            {
                KuFi.Engine.Services.WatchdogService.IsEnabled = false;
            }

            InitializeComponent();

            // KONFIGURASI SYSTEM TRAY (Run in Background)
            _notifyIcon = new NotifyIcon();
            try
            {
                var iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/logo.ico")).Stream;
                _notifyIcon.Icon = new System.Drawing.Icon(iconStream);
            }
            catch 
            {
                _notifyIcon.Icon = SystemIcons.Shield; // Fallback jika logo.ico tidak ditemukan
            }
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "KuFi AnVirs - Real-Time Guard Active";
            _notifyIcon.MouseClick += NotifyIcon_MouseClick;
        }

        private bool _forceExit = false;

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_forceExit && KuFi.UI.ViewModels.SettingsManager.Current.MinimizeToTray)
            {
                // Mencegah aplikasi benar-benar tertutup saat diklik tombol [X]
                e.Cancel = true;
                this.Hide(); // Menyembunyikan jendela ke latar belakang (RAM berkurang drastis karena GC WPF)
                
                // Memberikan notifikasi pop-up di pojok kanan bawah
                _notifyIcon.ShowBalloonTip(2000, "KuFi Active", "KuFi AnVirs akan tetap berjalan di latar belakang (System Tray) menggunakan memori minimal (<100MB) untuk melindungi PC Anda.", ToolTipIcon.Info);
            }
            else
            {
                // Tutup aplikasi sepenuhnya
                ExitApplication();
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        private void NotifyIcon_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowWindow();
            }
            else if (e.Button == MouseButtons.Right)
            {
                var cm = this.FindResource("TrayMenu") as System.Windows.Controls.ContextMenu;
                if (cm != null)
                {
                    cm.IsOpen = true;
                    // Hack to make sure the ContextMenu closes if clicked outside
                    SetForegroundWindow(new WindowInteropHelper(this).Handle);
                }
            }
        }

        private void TrayScan_Click(object sender, RoutedEventArgs e)
        {
            AutoStartScan = true;
            ShowWindow();
            if (this.DataContext is ViewModels.MainViewModel vm)
            {
                vm.NavigateDashboardCommand.Execute(null);
            }
            NavDashboard.IsChecked = true; // Update sidebar icon active state

            // Jika Frame sudah merender DashboardPage (karena tidak reload), trigger scan manual
            if (MainFrame.Content is Views.DashboardPage dashboardPage)
            {
                AutoStartScan = false;
                dashboardPage.TriggerScan();
            }
        }

        private void TraySettings_Click(object sender, RoutedEventArgs e)
        {
            ShowWindow();
            if (this.DataContext is ViewModels.MainViewModel vm)
            {
                vm.NavigateSettingsCommand.Execute(null);
            }
            NavSettings.IsChecked = true; // Update sidebar icon active state
        }

        private void TrayAbout_Click(object sender, RoutedEventArgs e)
        {
            var about = new Views.AboutWindow();
            about.ShowDialog();
        }

        private void TrayExit_Click(object sender, RoutedEventArgs e)
        {
            ExitApplication();
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate(); // Membawa jendela ke paling depan
        }

        private void ExitApplication()
        {
            _forceExit = true; // Bypass proteksi OnClosing
            
            // Membersihkan memori system tray icon sebelum mati total
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            System.Windows.Application.Current.Shutdown();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Akan memicu OnClosing, yang selanjutnya akan me-minimize ke tray jika setting aktif
        }
    }
}
