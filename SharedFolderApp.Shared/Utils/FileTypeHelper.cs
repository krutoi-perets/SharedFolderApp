namespace SharedFolderApp.Shared.Utils
{
    public class FileTypeHelper
    {
        private static readonly HashSet<string> TextExtensions =
            new HashSet<string>
            {
                ".txt",
                ".cs",
                ".cpp",
                ".c",
                ".h",
                ".hpp",
                ".pl",
                ".json",
                ".xml",
                ".xaml",
                ".html",
                ".css",
                ".js",
                ".ts",
                ".sql",
                ".md",
                ".ini",
                ".cfg",
                ".log"
            };

        public static bool IsTextFile(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            return TextExtensions.Contains(extension);
        }
    }
}
