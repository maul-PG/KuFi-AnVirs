using System.Windows.Controls;

namespace KuFi.UI.Views
{
    public partial class LogsPage : Page
    {
        public LogsPage()
        {
            InitializeComponent();
            
            // Binding otomatis ke global state
            LogGrid.ItemsSource = ViewModels.MainViewModel.ActivityLogs;
        }

        private void BtnClearLogs_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ViewModels.MainViewModel.ActivityLogs.Clear();
        }
    }
}
