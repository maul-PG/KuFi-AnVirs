namespace KuFi.Data.Models
{
    /// <summary>
    /// Merepresentasikan data ancaman (virus/malware) untuk tabel threat_library.
    /// Dibuat ringan tanpa anotasi EF Core untuk menghemat memori.
    /// </summary>
    public class Threat
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Hash MD5 atau SHA256 dari file malware.
        /// </summary>
        public string Hash { get; set; } = string.Empty;
        
        /// <summary>
        /// Nama ancaman (misal: Trojan.Win32.Generic).
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Jenis ancaman (Worm, Trojan, Ransomware, dll).
        /// </summary>
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// Tingkat bahaya (misal: 1 - 5).
        /// </summary>
        public int Severity { get; set; }
    }
}
