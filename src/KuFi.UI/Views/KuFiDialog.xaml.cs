using System.Windows;
using System.Windows.Media;

namespace KuFi.UI.Views
{
    public enum KuFiDialogButtons
    {
        Ok,
        YesNo
    }

    public enum KuFiDialogIcon
    {
        Info,
        Warning,
        Error,
        Success
    }

    public partial class KuFiDialog : Window
    {
        public bool Result { get; private set; } = false;

        public KuFiDialog(string title, string message, KuFiDialogButtons buttons, KuFiDialogIcon icon)
        {
            InitializeComponent();
            
            TxtTitle.Text = title;
            TxtMessage.Text = message;

            if (buttons == KuFiDialogButtons.YesNo)
            {
                BtnOk.Visibility = Visibility.Collapsed;
                BtnYes.Visibility = Visibility.Visible;
                BtnNo.Visibility = Visibility.Visible;
            }

            switch (icon)
            {
                case KuFiDialogIcon.Info:
                    IconText.Text = "\xE946"; // Info Icon
                    IconText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")); // Blue
                    break;
                case KuFiDialogIcon.Warning:
                    IconText.Text = "\xE7BA"; // Warning Icon
                    IconText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")); // Yellow
                    break;
                case KuFiDialogIcon.Error:
                    IconText.Text = "\xEA39"; // Error/Cancel Icon
                    IconText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")); // Red
                    break;
                case KuFiDialogIcon.Success:
                    IconText.Text = "\xE73E"; // Checkmark Icon
                    IconText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")); // Green
                    break;
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            this.Close();
        }

        private void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            this.Close();
        }

        private void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            this.Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            this.Close();
        }
    }
}
