using System;

namespace KuFi.Data.Models
{
    /// <summary>
    /// Merepresentasikan riwayat pemindaian untuk tabel scan_logs.
    /// </summary>
    public class ScanLog
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Waktu saat pemindaian dilakukan.
        /// </summary>
        public DateTime ScanTime { get; set; }
        
        /// <summary>
        /// Tipe pemindaian (Full, Quick, Custom, Rescue).
        /// </summary>
        public string ScanType { get; set; } = string.Empty;
        
        /// <summary>
        /// Jumlah file yang telah dipindai.
        /// </summary>
        public int FilesScanned { get; set; }
        
        /// <summary>
        /// Jumlah ancaman yang ditemukan selama pemindaian.
        /// </summary>
        public int ThreatsFound { get; set; }
        
        /// <summary>
        /// Status pemindaian (misal: Completed, Cancelled, Failed).
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }
}
