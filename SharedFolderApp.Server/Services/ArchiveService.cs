using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using System.IO.Compression;

namespace SharedFolderApp.Server.Services
{
    public class ArchiveService
    {
        public byte[] CreateZip(string directoryPath)
        {
            using (var memoryStream = new MemoryStream())
            {
                ZipFile.CreateFromDirectory(directoryPath, memoryStream);

                return memoryStream.ToArray();
            }
        }
    }
}
