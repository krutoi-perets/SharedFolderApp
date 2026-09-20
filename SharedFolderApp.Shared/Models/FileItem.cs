using System.Security.Cryptography;

namespace SharedFolderApp.Shared.Models
{
    public class FileItem
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public string Type {
            get
            {
                return IsDirectory ? "Папка" : "Файл";
            }
        }
        public long Size { get; set; }
        public string FormattedSize
        {
            get
            {
                if (IsDirectory) return "";

                if (Size < 1024) return $"{Size} байт";
                if (Size < 1024 * 1024) return $"{Size / 1024.0:F1} КБ";
                if (Size < 1024 * 1024 * 1024) return $"{Size / (1024.0 * 1024):F1} МБ";

                return $"{Size / (1024.0 * 1024 * 1024):F1} ГБ";
            }
        }
        public DateTime LastModified { get; set; }
    }
}
