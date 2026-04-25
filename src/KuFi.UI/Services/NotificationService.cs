using Microsoft.Toolkit.Uwp.Notifications;

namespace KuFi.UI.Services
{
    public static class NotificationService
    {
        public static void ShowToast(string title, string message)
        {
            try
            {
                new ToastContentBuilder()
                    .AddText(title)
                    .AddText(message)
                    .Show();
            }
            catch { }
        }
    }
}
