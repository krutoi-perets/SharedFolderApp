using SharedFolderApp.Shared.Models;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace SharedFolderApp.Client.Services
{
    public class ClientFileService
    {
        private readonly string _rootPath;

        public ClientFileService()
        {
            _rootPath = Path.Combine(AppContext.BaseDirectory, "ClientFolder");

            Directory.CreateDirectory(_rootPath);
        }

        public string GetFullPath(string relativePath)
        {
            return Path.Combine(_rootPath, relativePath);
        }

        public bool Exists(string relativePath)
        {
            var fullPath = GetFullPath(relativePath);

            return File.Exists(fullPath) || Directory.Exists(fullPath);
        }

        public List<FileItem> GetItems(string relativePath = "")
        {
            var fullPath = GetFullPath(relativePath);

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

        public void ClearDirectory(string relativePath)
        {
            var directoryPath = GetFullPath(relativePath);

            if (!Directory.Exists(directoryPath))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(directoryPath))
                File.Delete(file);

            foreach (var directory in Directory.GetDirectories(directoryPath))
                Directory.Delete(directory, true);
        }

        public async Task SaveFileAsync(string relativePath, Stream input, long? totalBytes, IProgress<double> progress)
        {
            var filePath = GetFullPath(relativePath);

            var directory = Path.GetDirectoryName(filePath);

            if (directory != null)
                Directory.CreateDirectory(directory);

            using var output = File.Create(filePath);

            var buffer = new byte[81920];
            long downloaded = 0;
            int bytesRead;

            while ((bytesRead = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await output.WriteAsync(buffer, 0, bytesRead);

                downloaded += bytesRead;

                if (totalBytes.HasValue)
                {
                    progress.Report((double)downloaded / totalBytes.Value * 100);
                }
            }
        }

        public async Task SaveZipAsync(string relativePath, Stream input, long? totalBytes, IProgress<double> progress)
        {
            var directoryPath = GetFullPath(relativePath);

            Directory.CreateDirectory(directoryPath);

            var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");

            using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write))
            {
                var buffer = new byte[81920];
                long downloaded = 0;
                int bytesRead;

                while ((bytesRead = await input.ReadAsync(buffer, 0 , buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);

                    downloaded += bytesRead;

                    if (totalBytes.HasValue)
                    {
                        progress.Report((double)downloaded / totalBytes.Value * 100);
                    }
                }
            }

            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, directoryPath, true);

            File.Delete(zipPath);
        }

        public List<string> GetFilesRecursive(string relativePath)
        {
            var fullPath = GetFullPath(relativePath);

            return Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories)
                        .Select(path => Path.GetRelativePath(_rootPath, path))
                        .ToList();
        }

        public List<string> GetDirectoriesRecursive(string relativePath)
        {
            var fullPath = GetFullPath(relativePath);

            return Directory.GetDirectories(fullPath, "*", SearchOption.AllDirectories)
                        .Select(path => Path.GetRelativePath(_rootPath, path))
                        .ToList();
        }

        public void Open(string relativePath)
        {
            var fullPath = GetFullPath(relativePath);

            Process.Start(new ProcessStartInfo
            {
                FileName = fullPath,
                UseShellExecute = true
            });
        }

        public void OpenInExplorer(string relativePath)
        {
            var fullPath = GetFullPath(relativePath);

            if (Directory.Exists(fullPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{fullPath}\"",
                    UseShellExecute = true
                });
            }
        }

        public void CreateFolder(string relativePath, string name)
        {
            var fullPath = GetFullPath(Path.Combine(relativePath, name));

            Directory.CreateDirectory(fullPath);
        }

        public void CreateFile(string relativePath, string name)
        {
            var fullPath = GetFullPath(Path.Combine(relativePath, name));

            if (File.Exists(fullPath))
                throw new Exception($"Файл с именем {name} уже существует");

            File.Create(fullPath).Dispose();
        }

        public void Rename(string relativePath, string newName)
        {
            var fullPath = GetFullPath(relativePath);

            if (File.Exists(fullPath))
            {
                var directory = Path.GetDirectoryName(fullPath);

                if (directory == null)
                    return;

                var newPath = Path.Combine(directory, newName);

                if (File.Exists(newPath))
                    throw new IOException($"File with name {newName} already exists");

                File.Move(fullPath, newPath);
                return;
            }

            if (Directory.Exists(fullPath))
            {
                var directory = Path.GetDirectoryName(fullPath);

                if (directory == null)
                    return;

                var newPath = Path.Combine(directory, newName);

                if (Directory.Exists(newPath))
                    throw new IOException($"Directory with name {newName} already exists");

                Directory.Move(fullPath, newPath);
            }
        }

        public void Delete(string relativePath)
        {
            var fullPath = GetFullPath(relativePath);

            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
                return;
            }

            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }
}
