using SharedFolderApp.Shared.Models;

namespace SharedFolderApp.Server.Services
{
    public class FileService
    {
        private readonly string _rootPath;

        public FileService(string rootPath)
        {
            _rootPath = rootPath;
        }

        public string? GetFilePath(string relativePath)
        {
            var fullPath = GetFullPath(relativePath);

            if (!File.Exists(fullPath))
                return null;

            return fullPath;
        }

        public string? GetDirectoryPath(string relativePath)
        {
            var fullPath = GetFullPath(relativePath);

            if (!Directory.Exists(fullPath))
                return null;

            return fullPath;
        }

        private string GetFullPath(string relativePath)
        {
            var rootPath = Path.GetFullPath(_rootPath).
                           TrimEnd(Path.DirectorySeparatorChar)
                           + Path.DirectorySeparatorChar;

            var fullPath = Path.GetFullPath(Path.Combine(rootPath, relativePath));

            if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Access denied");

            return fullPath;
        }

        public List<FileItem>? GetItems(string relativePath = "")
        {
            var fullPath = GetFullPath(relativePath);

            if (!Directory.Exists(fullPath))
                return null;

            var result = new List<FileItem>();
            
            foreach (var directory in Directory.GetDirectories(fullPath))
            {
                var directoryInfo = new DirectoryInfo(directory);
                
                result.Add(new FileItem
                {
                    Name = Path.GetFileName(directory),
                    Path = Path.GetRelativePath(_rootPath, directory),
                    IsDirectory = true,
                    Size = 0,
                    LastModified = directoryInfo.LastWriteTime
                });
            }

            foreach (var file in Directory.GetFiles(fullPath))
            {
                var fileInfo = new FileInfo(file);

                result.Add(new FileItem
                {
                    Name = Path.GetFileName(file),
                    Path = Path.GetRelativePath(_rootPath, file),
                    IsDirectory = false,
                    Size = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime
                });
            }

            return result;
        }

        public async Task SaveChunkAsync(
            Stream chunkStream,
            string relativePath,
            int chunkNumber)
        {
            var fullPath = GetFullPath(relativePath);

            var directory = Path.GetDirectoryName(fullPath);

            if (directory != null)
                Directory.CreateDirectory(directory);

            var mode = chunkNumber == 0
                ? FileMode.Create
                : FileMode.Append;

            await using var output = new FileStream(
                fullPath,
                mode,
                FileAccess.Write,
                FileShare.None);

            await chunkStream.CopyToAsync(output);
        }

        public void CreateDirectory(string relativePath)
        {
            var fullPath = GetFullPath(relativePath);

            if (Directory.Exists(fullPath))
                throw new IOException("Directory already exists");

            Directory.CreateDirectory(fullPath);
        }

        public void CreateFile(string relativePath)
        {
            var fullPath = GetFullPath(relativePath);

            if (File.Exists(fullPath))
                throw new IOException("File already exists");

            File.Create(fullPath).Dispose();
        }

        public async Task<string?> GetTextAsync(string relativePath)
        {
            var fullPath = GetFullPath(relativePath);

            if (!File.Exists(fullPath))
                return null;

            return await File.ReadAllTextAsync(fullPath);
        }

        public void Rename(string relativePath, string newName)
        {
            var fullPath = GetFullPath(relativePath);

            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
                throw new FileNotFoundException();

            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("New name is required");

            var parentDirectory = Path.GetDirectoryName(fullPath)!;
            var newPath = Path.Combine(parentDirectory, newName);

            if (File.Exists(newPath) || Directory.Exists(newPath))
                throw new IOException("File or directory with this name already exists");

            if (File.Exists(fullPath))
                File.Move(fullPath, newPath);
            else
                Directory.Move(fullPath, newPath);
        }

        public bool Delete(string relativePath)
        {
            var fullPath = GetFullPath(relativePath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }

            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
                return true;
            }

            return false;
        }
    }
}
