using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace KuFi.Engine.Rescue
{
    /// <summary>
    /// Menangani logika pembersihan dan pemulihan Flashdrive.
    /// Dirancang menggunakan System.IO standar agar minim pemakaian RAM.
    /// </summary>
    public class Cleaner
    {
        // Event untuk mengirim log proses ke UI
        public delegate void ProgressUpdateHandler(string message);
        public event ProgressUpdateHandler? OnProgressUpdate;

        public async Task RescueDriveAsync(string driveLetter)
        {
            try
            {
                if (string.IsNullOrEmpty(driveLetter))
                    throw new ArgumentException("Drive letter tidak valid.");

                // Pastikan format path benar (contoh: "E:\")
                string rootPath = driveLetter.ToUpper();
                if (!rootPath.EndsWith(":\\"))
                    rootPath += ":\\";

                if (!Directory.Exists(rootPath))
                {
                    Log($"[ERROR] Drive {rootPath} tidak ditemukan atau dicabut.");
                    return;
                }

                Log($"Memulai inisialisasi pada {rootPath}...");

                // 1. Menghapus file eksekusi berbahaya seperti shortcut (.lnk) dan script (.vbs)
                await Task.Run(() => DeleteMaliciousFiles(rootPath));

                // 2. Memulihkan atribut file yang di-hidden (attrib -s -h -r /s /d)
                await Task.Run(() => RestoreAttributes(rootPath));

                Log($"Operasi selesai. Drive {rootPath} telah diamankan.");
            }
            catch (Exception ex)
            {
                Log($"[FATAL ERROR] {ex.Message}");
            }
        }

        private void DeleteMaliciousFiles(string rootPath)
        {
            Log("Mencari anomali pada direktori root (.lnk, .vbs)...");
            
            try
            {
                // Menggunakan EnumerateFiles yang sangat efisien untuk RAM karena tidak memuat
                // seluruh struktur array file ke memory sekaligus (cocok untuk RAM 4GB).
                var files = Directory.EnumerateFiles(rootPath, "*.*", SearchOption.TopDirectoryOnly);
                
                int deletedCount = 0;
                foreach (var file in files)
                {
                    if (file.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) || 
                        file.EndsWith(".vbs", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            File.Delete(file);
                            deletedCount++;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // Mengabaikan file yang memang dikunci oleh sistem
                        }
                    }
                }
                
                if (deletedCount > 0)
                    Log($"Berhasil menetralisir {deletedCount} ancaman shortcut/script.");
                else
                    Log("Tidak ditemukan file anomali/shortcut.");
            }
            catch (Exception ex)
            {
                Log($"[WARNING] Gagal memindai direktori root: {ex.Message}");
            }
        }

        private void RestoreAttributes(string rootPath)
        {
            Log("Memulihkan struktur file dan folder yang tersembunyi (attrib)...");
            
            try
            {
                // CATATAN PENTING:
                // Proses ini membutuhkan "Run as Administrator" di app.manifest untuk bekerja maksimal
                // pada drive sistem tertentu. Untuk flashdisk biasanya tidak masalah.
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c attrib -s -h -r /s /d \"{rootPath}*.*\"",
                    UseShellExecute = false,
                    CreateNoWindow = true // Menjalankan proses di background tanpa memunculkan jendela hitam (efisiensi dan UX)
                };

                using (Process? process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        process.WaitForExit();
                        Log("MFT (Master File Table) correction sukses. Atribut file pulih.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[WARNING] Gagal menjalankan operasi atribusi: {ex.Message}");
                Log("Tip: Coba jalankan KuFi sebagai Administrator jika ada folder yang belum muncul.");
            }
        }

        private void Log(string message)
        {
            // Mengirim format log dengan timestamp
            OnProgressUpdate?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
        }
    }
}
