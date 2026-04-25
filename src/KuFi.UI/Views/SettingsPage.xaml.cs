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
            chkHeuristic.IsChecked = SettingsManager.Current.UseHeuristicEngine;
            chkQuarantine.IsChecked = SettingsManager.Current.AutoQuarantine;
            chkWatchdog.IsChecked = SettingsManager.Current.EnableWatchdog;
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            // Apply checkbox states to Current configuration
            SettingsManager.Current.RunAsAdmin = chkRunAdmin.IsChecked ?? true;
            SettingsManager.Current.RealTimeMonitor = chkRealTimeMonitor.IsChecked ?? true;
            SettingsManager.Current.MinimizeToTray = chkMinimizeToTray.IsChecked ?? true;
            SettingsManager.Current.UseHeuristicEngine = chkHeuristic.IsChecked ?? true;
            SettingsManager.Current.AutoQuarantine = chkQuarantine.IsChecked ?? true;
            SettingsManager.Current.EnableWatchdog = chkWatchdog.IsChecked ?? true;
            
            // Write to JSON
            SettingsManager.Save();

            // Apply Settings Immediately (e.g. toggle Dashboard monitor)
            // (Dashboard monitor actually checks SettingsManager.Current.RealTimeMonitor natively now or we could trigger an event)
            SettingsManager.ApplyStartupLogic();

            KuFi.Engine.Services.WatchdogService.IsEnabled = SettingsManager.Current.EnableWatchdog;
            
            if (SettingsManager.Current.EnableWatchdog)
                KuFi.Engine.Services.WatchdogService.StartWatchdog();
            else
                KuFi.Engine.Services.WatchdogService.StopWatchdog();

            var dialog = new KuFiDialog("Settings Saved", "Your application preferences have been updated successfully.", KuFiDialogButtons.Ok, KuFiDialogIcon.Info);
            dialog.ShowDialog();
        }

        private void BtnRepair_Click(object sender, RoutedEventArgs e)
        {
            KuFi.Engine.Services.SystemRepair.FixSystemPolicies();
            KuFi.Engine.Services.SystemRepair.FixHiddenFiles();

            var dialog = new KuFiDialog("System Repair", "Windows policies and visibility settings have been restored successfully.", KuFiDialogButtons.Ok, KuFiDialogIcon.Success);
            dialog.ShowDialog();
        }
    }
}
