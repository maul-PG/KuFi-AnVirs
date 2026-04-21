using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using KuFi.Data;

namespace KuFi.Engine.Scanners
{
    /// <summary>
    /// Modul inti untuk memindai sidik jari (Hash) dari sebuah file.
    /// Dirancang menggunakan streaming untuk menghindari lonjakan pemakaian RAM.
    /// </summary>
    public class HashScanner
    {
        private readonly SQLiteContext _dbContext;

        public HashScanner()
        {
            // Menghubungkan scanner ke database internal KuFi
            _dbContext = new SQLiteContext();
        }

        /// <summary>
        /// Menghitung nilai MD5 dari file sesuai permintaan database (EICAR).
        /// Efisiensi: FileStream membaca blok demi blok, bukan meload seluruh file ke RAM (Aman untuk laptop RAM 4GB).
        /// </summary>
        public async Task<string> CalculateMD5Async(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                // Buffer size disetel ke 80KB yang sangat optimal dan ringan
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 81920, true))
                {
                    byte[] hashBytes = await md5.ComputeHashAsync(stream);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        /// <summary>
        /// Mengecek apakah file tersebut berbahaya berdasarkan Heuristics Pattern dan threat_library.
        /// </summary>
        public async Task<(bool isInfected, string threatName, string fileHash)> CheckThreatAsync(string filePath)
        {
            string fileHash = string.Empty;
            try
            {
                // 1. Dapatkan hash MD5 dari file target
                fileHash = await CalculateMD5Async(filePath);

                // 2. SMART HEURISTIC SCAN (Mencari pola file mencurigakan tanpa DB)
                string fileName = Path.GetFileName(filePath).ToLower();
                string extension = Path.GetExtension(fileName);
                
                // Cek double extension (contoh: virus.txt.exe)
                if (fileName.EndsWith(".txt.exe") || fileName.EndsWith(".pdf.exe") || fileName.EndsWith(".jpg.vbs"))
                {
                    return (true, "Heuristics.SuspiciousDoubleExtension", fileHash);
                }
                
                // Cek file EXE yang bersembunyi di folder yang tidak wajar
                if ((extension == ".exe" || extension == ".bat" || extension == ".vbs") && 
                    (filePath.Contains("$RECYCLE.BIN") || filePath.Contains("System Volume Information")))
                {
                    return (true, "Heuristics.HiddenExecutable", fileHash);
                }

                // 3. Bandingkan dengan database lokal SQLite
                using (var connection = _dbContext.GetConnection())
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        // Mencari data yang cocok di kolom Hash, ambil Nama Ancamannya
                        command.CommandText = "SELECT Name FROM threat_library WHERE Hash = @hash LIMIT 1";
                        command.Parameters.AddWithValue("@hash", fileHash);

                        var result = await command.ExecuteScalarAsync();
                        
                        // Jika hasilnya tidak null, berarti file ini terdaftar sebagai ancaman (Infected)
                        if (result != null)
                        {
                            return (true, result.ToString(), fileHash);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HashScanner Error] Gagal memindai file {filePath}: {ex.Message}");
            }

            return (false, string.Empty, fileHash); // Status: Safe / Clean
        }
    }
}
