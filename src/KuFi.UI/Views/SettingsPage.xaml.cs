using System.Windows;
using System.Windows.Controls;
using KuFi.UI.ViewModels;

namespace KuFi.UI.Views
{
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
            this.Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Reset to saved settings whenever page is loaded (reverting unsaved changes)
            chkRunAdmin.IsChecked = SettingsManager.Current.RunAsAdmin;
            chkRealTimeMonitor.IsChecked = SettingsManager.Current.RealTimeMonitor;
            chkMinimizeToTray.IsChecked = SettingsManager.Current.MinimizeToTray;
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            // Apply checkbox states to Current configuration
            SettingsManager.Current.RunAsAdmin = chkRunAdmin.IsChecked ?? true;
            SettingsManager.Current.RealTimeMonitor = chkRealTimeMonitor.IsChecked ?? true;
            SettingsManager.Current.MinimizeToTray = chkMinimizeToTray.IsChecked ?? true;
            
            // Write to JSON
            SettingsManager.Save();

            // Apply Settings Immediately (e.g. toggle Dashboard monitor)
            // (Dashboard monitor actually checks SettingsManager.Current.RealTimeMonitor natively now or we could trigger an event)

            var dialog = new KuFiDialog("Settings Saved", "Your application preferences have been updated successfully.", KuFiDialogButtons.Ok, KuFiDialogIcon.Info);
            dialog.ShowDialog();
        }
    }
}
