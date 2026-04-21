using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace KuFi.UI.Views
{
    public partial class NotificationWindow : Window
    {
        private DispatcherTimer _closeTimer;
        public string TargetFilePath { get; set; }

        public NotificationWindow(string fileName, string fullPath)
        {
            InitializeComponent();
            TxtFileName.Text = fileName;
            TargetFilePath = fullPath;

            // Auto-close dalam 10 detik
            _closeTimer = new DispatcherTimer();
            _closeTimer.Interval = TimeSpan.FromSeconds(10);
            _closeTimer.Tick += (s, e) => { 
                _closeTimer.Stop();
                this.Close(); 
            };
            _closeTimer.Start();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Posisikan di Pojok Kanan Bawah layar (di atas Taskbar)
            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width - 10;
            this.Top = desktopWorkingArea.Bottom - this.Height - 10;
        }

        private void BtnAbaikan_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnHapus_Click(object sender, RoutedEventArgs e)
        {
            try 
            { 
                if (File.Exists(TargetFilePath))
                {
                    File.Delete(TargetFilePath); 
                }
            } 
            catch { }
            
            this.Close();
        }
    }
}
