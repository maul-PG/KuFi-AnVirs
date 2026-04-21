using System;
using System.Collections.Generic;
using System.IO;

namespace KuFi.Engine.Rescue
{
    /// <summary>
    /// Modul deteksi *Hardware* untuk mengidentifikasi partisi Flashdrive (Removable Media).
    /// Dipisahkan dari UI logic agar sesuai dengan arsitektur Engine KuFi.
    /// </summary>
    public class DriveDetector
    {
        // Struktur data sederhana untuk membawa detail partisi
        public class DriveDetails
        {
            public string Name { get; set; } = string.Empty;
            public string Letter { get; set; } = string.Empty;
            public string Details { get; set; } = string.Empty;
        }

        /// <summary>
        /// Mengambil daftar drive USB atau media yang dapat dilepas secara instan.
        /// </summary>
        public List<DriveDetails> GetRemovableDrives()
        {
            var detectedDrives = new List<DriveDetails>();
            
            try
            {
                // Menggunakan System.IO murni, tidak perlu instansiasi WMI yang lambat
                DriveInfo[] allDrives = DriveInfo.GetDrives();
                
                foreach (DriveInfo d in allDrives)
                {
                    // Memfilter drive yang berjenis Removable (Flashdisk)
                    // (Kita juga sisipkan Fixed drive untuk testing sementara, Anda bisa hapus 'd.DriveType == DriveType.Fixed' jika mau murni USB saja)
                    if (d.IsReady && (d.DriveType == DriveType.Removable || d.DriveType == DriveType.Fixed))
                    {
                        string volumeLabel = string.IsNullOrWhiteSpace(d.VolumeLabel) ? "LOCAL_DISK" : d.VolumeLabel;
                        
                        detectedDrives.Add(new DriveDetails
                        {
                            Name = $"{volumeLabel} ({d.Name.TrimEnd('\\')})",
                            Letter = d.Name,
                            Details = $"{d.TotalSize / (1024 * 1024 * 1024)} GB • {d.DriveFormat}" // e.g: 32 GB • FAT32
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DriveDetector] Terjadi kesalahan: {ex.Message}");
            }
            
            return detectedDrives;
        }
    }
}
