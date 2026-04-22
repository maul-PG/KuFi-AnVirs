using System.Diagnostics;
using System.Windows;

namespace KuFi.UI.Views
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Github_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Membuka link GitHub di browser default
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/maul-PG",
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }
}
