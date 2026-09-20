using SharedFolderApp.Shared.Models;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Windows;
using System.Windows.Markup;

namespace SharedFolderApp.Client.Services
{
    public class ServerService
    {
        private HttpClient _httpClient;

        public bool IsConnected {  get; private set; }

        public ServerService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<bool> ConnectAsync(string ip)
        {
            try
            {
                if (IsConnected)
                    await DisconnectAsync();

                _httpClient?.Dispose();
                _httpClient = new HttpClient();

                var address = $"http://{ip}:5212/";

                _httpClient.BaseAddress = new Uri(address);

                var response = await _httpClient.GetAsync("api/files/status");

                IsConnected = response.IsSuccessStatusCode;
            }
            catch
            {
                IsConnected = false;
            }

            return IsConnected;
        }

        public async Task DisconnectAsync()
        {
            try
            {
                await _httpClient.PostAsync("api/files/disconnect", null);
            }
            catch
            {

            }

            IsConnected = false;
        }

        public async Task<List<FileItem>> GetFilesAsync(string path = "")
        {
            var response = await _httpClient.GetAsync(
                $"api/files?path={Uri.EscapeDataString(path)}");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<FileItem>>()
                ?? new List<FileItem>();
        }

        public async Task<(Stream Stream, long? TotalBytes)> DownloadAsync(string path)
        {
            var response = await _httpClient.GetAsync($"api/files/download?path={Uri.EscapeDataString(path)}",
                                    HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode(); 

            var totalBytes = response.Content.Headers.ContentLength;
            var stream = await response.Content.ReadAsStreamAsync();

            return (stream, totalBytes);
        }

        public async Task<string> GetTextAsync(string path)
        {
            var response = await _httpClient.GetAsync($"api/files/text?path={Uri.EscapeDataString(path)}");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task CreateFolderAsync(string path)
        {
            var response = await _httpClient.PostAsync($"api/files/createFolder?path={Uri.EscapeDataString(path)}", null);

            response.EnsureSuccessStatusCode();
        }

        public async Task CreateFileAsync(string path)
        {
            var response = await _httpClient.PostAsync($"api/files/createFile?path={Uri.EscapeDataString(path)}", null);

            response.EnsureSuccessStatusCode();
        }

        public async Task UploadDirectoryAsync(
            string fromPath,
            string toPath,
            IProgress<double>? progress = null)
        {
            await CreateFolderAsync(toPath);

            foreach (var file in Directory.GetFiles(fromPath))
            {
                await UploadFileAsync(
                    file,
                    toPath,
                    progress);
            }

            foreach (var directory in Directory.GetDirectories(fromPath))
            {
                var directoryName = Path.GetFileName(directory);

                await UploadDirectoryAsync(
                    directory,
                    Path.Combine(toPath, directoryName),
                    progress);
            }
        }

        public async Task UploadFileAsync(
            string fromPath,
            string toPath,
            IProgress<double>? progress = null)
        {
            const int chunkSize = 8 * 1024 * 1024;

            var fileName = Path.GetFileName(fromPath);

            await using var stream = File.OpenRead(fromPath);

            if (stream.Length == 0)
            {
                using var content = new ByteArrayContent(Array.Empty<byte>());

                var response = await _httpClient.PostAsync(
                    $"api/files/uploadChunk" +
                    $"?path={Uri.EscapeDataString(toPath)}" +
                    $"&fileName={Uri.EscapeDataString(fileName)}" +
                    $"&chunkNumber=0",
                    content);

                response.EnsureSuccessStatusCode();

                progress?.Report(100);
                return;
            }

            byte[] buffer = new byte[chunkSize];
            int chunkNumber = 0;
            long uploadedBytes = 0;

            while (true)
            {
                int bytesRead = await stream.ReadAsync(
                    buffer,
                    0,
                    buffer.Length);

                if (bytesRead == 0)
                    break;

                using var content = new ByteArrayContent(
                    buffer,
                    0,
                    bytesRead);

                var response = await _httpClient.PostAsync(
                    $"api/files/uploadChunk" +
                    $"?path={Uri.EscapeDataString(toPath)}" +
                    $"&fileName={Uri.EscapeDataString(fileName)}" +
                    $"&chunkNumber={chunkNumber}",
                    content);

                response.EnsureSuccessStatusCode();

                uploadedBytes += bytesRead;

                double percent = (double)uploadedBytes / stream.Length * 100;
                progress?.Report(percent);

                chunkNumber++;
            }
        }

        public async Task RenameAsync(string path, string newName)
        {
            var response = await _httpClient.PutAsync(
                $"api/files/rename?path={Uri.EscapeDataString(path)}&newName={Uri.EscapeDataString(newName)}", null);

            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(string path)
        {
            var response = await _httpClient.DeleteAsync($"api/files?path={Uri.EscapeDataString(path)}");

            response.EnsureSuccessStatusCode();
        }
    }
}
