using Microsoft.Win32;

namespace KuFi.Engine.Services
{
    public static class SystemRepair
    {
        public static void FixSystemPolicies()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true))
                {
                    if (key != null)
                    {
                        key.DeleteValue("DisableTaskMgr", false);
                        key.DeleteValue("DisableRegistryTools", false);
                    }
                }

                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Policies\Microsoft\Windows\System", true))
                {
                    if (key != null)
                    {
                        key.DeleteValue("DisableCMD", false);
                    }
                }
            }
            catch { }
        }

        public static void FixHiddenFiles()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true))
                {
                    if (key != null)
                    {
                        key.SetValue("Hidden", 1, RegistryValueKind.DWord);
                        key.SetValue("ShowSuperHidden", 1, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }
        }
    }
}
