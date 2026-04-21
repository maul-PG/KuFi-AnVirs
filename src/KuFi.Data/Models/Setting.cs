namespace KuFi.Data.Models
{
    /// <summary>
    /// Merepresentasikan pengaturan pengguna untuk tabel user_settings.
    /// </summary>
    public class Setting
    {
        /// <summary>
        /// Kunci pengaturan (berlaku sebagai Primary Key).
        /// </summary>
        public string Key { get; set; } = string.Empty;
        
        /// <summary>
        /// Nilai pengaturan.
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }
}
