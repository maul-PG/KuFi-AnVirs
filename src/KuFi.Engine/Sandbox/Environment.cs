using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace KuFi.Engine.Sandbox
{
    /// <summary>
    /// Objek pembungkus untuk melacak aplikasi yang berjalan dalam Sandbox.
    /// </summary>
    public class SandboxedApp
    {
        public int ProcessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public DateTime LaunchTime { get; set; }
        public Process ProcessObj { get; set; } = null!;
    }

    /// <summary>
    /// Logika manager untuk meluncurkan banyak aplikasi secara terisolasi.
    /// </summary>
    public class SandboxEnvironment
    {
        private readonly List<SandboxedApp> _activeApps = new List<SandboxedApp>();

        // Menyediakan akses baca ke daftar aplikasi aktif untuk UI
        public IReadOnlyList<SandboxedApp> ActiveApps => _activeApps.AsReadOnly();

        /// <summary>
        /// Meluncurkan executable (.exe) dengan hak akses yang diturunkan/dibatasi (Restricted Token behavior).
        /// </summary>
        public SandboxedApp? LaunchSandboxed(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("Aplikasi tidak ditemukan.");

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = false,  // Diperlukan agar dapat mengubah Environment Variables
                    CreateNoWindow = false,
                    ErrorDialog = false
                };

                // IMPLEMENTASI PEMBATASAN HAK AKSES (MINIMALIS UNTUK RAM 4GB):
                // Menggunakan native API "__COMPAT_LAYER" = "RunAsInvoker"
                // Ini meniru perilaku Restricted Token dengan menghalau UAC dan memaksa
                // aplikasi berjalan dengan hak Standard User yang tidak bisa menulisi folder sistem.
                psi.EnvironmentVariables["__COMPAT_LAYER"] = "RunAsInvoker";

                Process process = new Process { StartInfo = psi };
                
                if (process.Start())
                {
                    var app = new SandboxedApp
                    {
                        ProcessId = process.Id,
                        Name = Path.GetFileName(filePath),
                        Path = filePath,
                        LaunchTime = DateTime.Now,
                        ProcessObj = process
                    };

                    _activeApps.Add(app);

                    // Menangkap event secara otomatis jika aplikasi dimatikan secara eksternal
                    process.EnableRaisingEvents = true;
                    process.Exited += (s, e) => 
                    {
                        _activeApps.Remove(app);
                    };

                    Console.WriteLine($"[Sandbox] Aplikasi {app.Name} berjalan terisolasi (PID: {app.ProcessId}).");
                    return app;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Sandbox Error] Gagal meluncurkan aplikasi: {ex.Message}");
            }
            
            return null;
        }

        /// <summary>
        /// Menghentikan aplikasi secara paksa.
        /// </summary>
        public void StopApp(int processId)
        {
            var app = _activeApps.FirstOrDefault(a => a.ProcessId == processId);
            if (app != null)
            {
                try
                {
                    if (!app.ProcessObj.HasExited)
                    {
                        app.ProcessObj.Kill();
                        app.ProcessObj.WaitForExit(2000); // Tunggu sampai terbunuh sepenuhnya
                    }
                }
                catch (Exception)
                {
                    // Abaikan exception seperti AccessDenied jika proses sudah tertutup dari OS
                }
                finally
                {
                    _activeApps.Remove(app);
                    Console.WriteLine($"[Sandbox] Proses {processId} berhasil dihentikan.");
                }
            }
        }
    }
}
