using System.IO;

namespace KuFi.Engine.Services
{
    public static class HeuristicEngine
    {
        public static bool IsHeuristicThreat(string filePath)
        {
            var info = new FileInfo(filePath);
            string ext = info.Extension.ToLowerInvariant();
            string lowerPath = filePath.ToLowerInvariant();

            // 1. Hidden in System Folders
            if ((ext == ".exe" || ext == ".vbs" || ext == ".lnk") &&
                (lowerPath.Contains("\\recycler\\") || lowerPath.Contains("\\system volume information\\")))
            {
                return true;
            }

            // 2. Folder Mimicking
            if (ext == ".exe")
            {
                if (info.DirectoryName != null)
                {
                    string dirName = new DirectoryInfo(info.DirectoryName).Name;
                    if (Path.GetFileNameWithoutExtension(filePath).Equals(dirName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            // 3. Suspicious Attributes (Hidden + System + Executable)
            if (ext == ".exe" || ext == ".vbs" || ext == ".scr")
            {
                if ((info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden &&
                    (info.Attributes & FileAttributes.System) == FileAttributes.System)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
