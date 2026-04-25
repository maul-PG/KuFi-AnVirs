using System.IO;

namespace KuFi.Engine.Services
{
    public static class QuarantineManager
    {
        private const byte XorKey = 0x42;
        private static readonly string QuarantinePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData), "KuFiAnVirs", "Quarantine");

        public static void QuarantineFile(string sourceFilePath)
        {
            if (!Directory.Exists(QuarantinePath))
                Directory.CreateDirectory(QuarantinePath);

            string fileName = Path.GetFileName(sourceFilePath);
            string destPath = Path.Combine(QuarantinePath, fileName + ".kufivirus");

            byte[] fileBytes = File.ReadAllBytes(sourceFilePath);
            for (int i = 0; i < fileBytes.Length; i++)
            {
                fileBytes[i] ^= XorKey;
            }

            File.WriteAllBytes(destPath, fileBytes);
            File.Delete(sourceFilePath);
        }
    }
}
