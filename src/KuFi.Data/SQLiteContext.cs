using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace KuFi.Data
{
    /// <summary>
    /// Konteks SQLite menggunakan ADO.NET murni (Microsoft.Data.Sqlite).
    /// Dirancang sangat ringan dan efisien memori sebagai ganti Entity Framework Core
    /// untuk memastikan aplikasi tetap cepat di sistem dengan RAM terbatas (misal 4GB).
    /// </summary>
    public class SQLiteContext
    {
        private readonly string _connectionString;

        public SQLiteContext(string dbFileName = "kufi_database.db")
        {
            // Menggunakan folder AppData/Local agar selalu memiliki izin Read/Write (menghindari error akses di Program Files)
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dbFolder = Path.Combine(appDataPath, "KuFi");
            
            if (!Directory.Exists(dbFolder))
            {
                Directory.CreateDirectory(dbFolder);
            }

            string fullDbPath = Path.Combine(dbFolder, dbFileName);
            
            // Atur mode pembuatan koneksi SQLite
            _connectionString = $"Data Source={fullDbPath};";

            // Inisialisasi database secara otomatis jika belum ada file atau tabel
            InitializeDatabase();
        }

        /// <summary>
        /// Mendapatkan objek SqliteConnection.
        /// Pastikan menggunakan blok 'using' saat memanggil method ini.
        /// </summary>
        public SqliteConnection GetConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        /// <summary>
        /// Membuat file database dan tabel-tabel utamanya jika belum ada.
        /// </summary>
        private void InitializeDatabase()
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    // Query untuk membuat tabel dengan tipe data SQLite
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS threat_library (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Hash TEXT NOT NULL UNIQUE,
                            Name TEXT NOT NULL,
                            Type TEXT NOT NULL,
                            Severity INTEGER NOT NULL
                        );

                        CREATE TABLE IF NOT EXISTS scan_logs (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            ScanTime TEXT NOT NULL,
                            ScanType TEXT NOT NULL,
                            FilesScanned INTEGER NOT NULL,
                            ThreatsFound INTEGER NOT NULL,
                            Status TEXT NOT NULL
                        );

                        CREATE TABLE IF NOT EXISTS user_settings (
                            Key TEXT PRIMARY KEY,
                            Value TEXT NOT NULL
                        );
                        
                        -- Data Seeding: EICAR MD5 Signature
                        INSERT OR IGNORE INTO threat_library (Hash, Name, Type, Severity) 
                        VALUES ('44d88612fea8a8f36de82e1278abb02f', 'EICAR-Test-Signature', 'Trojan', 3);
                    ";
                    
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
