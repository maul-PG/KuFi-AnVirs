using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms; // Butuh UseWindowsForms = true di .csproj
using System.IO;
using System.Threading.Tasks;

namespace KuFi.UI
{
    public partial class MainWindow : Window
    {
        private NotifyIcon _notifyIcon;

        public MainWindow()
        {
            InitializeComponent();

            // KONFIGURASI SYSTEM TRAY (Run in Background)
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = SystemIcons.Shield; // Menggunakan ikon tameng bawaan Windows
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "KuFi AnVirs - Protection Active";
            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            // Menambahkan Context Menu (Klik Kanan pada Ikon di Pojok Kanan Bawah)
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Open Dashboard", null, (s, e) => ShowWindow());
            contextMenu.Items.Add("Exit KuFi", null, (s, e) => ExitApplication());
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Mencegah aplikasi benar-benar tertutup saat diklik tombol [X]
            e.Cancel = true;
            this.Hide(); // Menyembunyikan jendela ke latar belakang (RAM berkurang drastis karena GC WPF)
            
            // Memberikan notifikasi pop-up di pojok kanan bawah
            _notifyIcon.ShowBalloonTip(2000, "KuFi Active", "KuFi AnVirs akan tetap berjalan di latar belakang (System Tray) menggunakan memori minimal (<100MB) untuk melindungi PC Anda.", ToolTipIcon.Info);
        }

        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            ShowWindow();
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate(); // Membawa jendela ke paling depan
        }

        private void ExitApplication()
        {
            // Membersihkan memori system tray icon sebelum mati total
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }
    }
}
