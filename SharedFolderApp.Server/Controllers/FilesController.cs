using Microsoft.AspNetCore.Mvc;
using SharedFolderApp.Server.Logger;
using SharedFolderApp.Server.Services;

namespace SharedFolderApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly FileService _fileService;
        private readonly ArchiveService _archiveService;

        public FilesController(FileService fileService,
                               ArchiveService archiveService)
        {
            _fileService = fileService;
            _archiveService = archiveService;
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            ConsoleLogger.LogInfo($"Клиент {HttpContext.Connection.RemoteIpAddress} подключился");

            return Ok();
        }

        [HttpPost("disconnect")]
        public IActionResult Disconnect()
        {
            ConsoleLogger.LogInfo($"Клиент {HttpContext.Connection.RemoteIpAddress} отключился");

            return Ok();
        }

        [HttpGet]
        public IActionResult GetFiles(string path = "")
        {
            var files = _fileService.GetItems(path);

            if (files == null)
                return NotFound("Directory not found");

            return Ok(files);
        }

        [HttpGet("download")]
        public IActionResult Download(string path)
        {
            var filePath = _fileService.GetFilePath(path);

            if (filePath != null)
            {
                var fileName = Path.GetFileName(filePath);

                return PhysicalFile(filePath, "application/octet-stream", fileName);
            }

            var directoryPath = _fileService.GetDirectoryPath(path);

            if (directoryPath != null)
            {
                var zip = _archiveService.CreateZip(directoryPath);

                var directoryName = Path.GetFileName(directoryPath.TrimEnd(Path.DirectorySeparatorChar));

                return File(zip, "application/zip", $"{directoryName}.zip");
            }


            return NotFound("File or directrory not found");
        }

        [HttpGet("text")]
        public async Task<IActionResult> GetText(string path)
        {
            var text = await _fileService.GetTextAsync(path);

            if (text == null)
                return NotFound("File not found");

            return Content(text, "text/plain");
        }

        [HttpPost("uploadChunk")]
        public async Task<IActionResult> Upload(
            [FromQuery] string? path,
            [FromQuery] string fileName,
            [FromQuery] int chunkNumber,
            [FromQuery] int totalChunks)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("File name is required");

            if (totalChunks <= 0 || chunkNumber < 0 || chunkNumber >= totalChunks)
                return BadRequest("Invalid chunk information");

            var filePath = string.IsNullOrWhiteSpace(path)
                ? fileName
                : Path.Combine(path, fileName);

            try
            {
                await _fileService.SaveChunkAsync(
                    Request.Body,
                    filePath,
                    chunkNumber);

                if (chunkNumber == totalChunks - 1)
                {
                    ConsoleLogger.LogInfo(
                        $"Клиент {HttpContext.Connection.RemoteIpAddress} загрузил файл Server\\{path}\\{fileName}");
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("createFolder")]
        public IActionResult CreateFolder([FromQuery] string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return BadRequest("Path is required");

            try
            {
                _fileService.CreateDirectory(path);
                ConsoleLogger.LogInfo($"Клиент {HttpContext.Connection.RemoteIpAddress} создал папку Server\\{path}");
                return Ok();
            }
            catch (IOException)
            {
                return Conflict("Directory already exists");
            }
        }

        [HttpPost("createFile")]
        public IActionResult CreateFile([FromQuery] string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return BadRequest("Path is required");

            try
            {
                _fileService.CreateFile(path);
                ConsoleLogger.LogInfo($"Клиент {HttpContext.Connection.RemoteIpAddress} создал файл Server\\{path}");
                return Ok();
            }
            catch (IOException)
            {
                return Conflict("File already exists");
            }
        }

        [HttpPut("rename")]
        public IActionResult Rename(string path, string newName)
        {
            try
            {
                _fileService.Rename(path, newName);
                ConsoleLogger.LogInfo($"Клиент {HttpContext.Connection.RemoteIpAddress} переименовал Server\\{path} на {newName}");
                return Ok();
            }
            catch (FileNotFoundException)
            {
                return NotFound("File or directory not found");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (IOException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpDelete]
        public IActionResult Delete(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return BadRequest("Path is required");

            if (!_fileService.Delete(path))
                return NotFound("File or directory not found");

            ConsoleLogger.LogInfo($"Клиент {HttpContext.Connection.RemoteIpAddress} удалил Server\\{path}");
            return Ok();
        }
    }
}
