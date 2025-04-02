using Microsoft.AspNetCore.Mvc;
using Website.Data;
using Website.Util;

namespace Website.Controllers
{
    public class GetAttachedFile(DatabaseContext dbContext) : Controller
    {
        [Route("/attached-file/{guid}")]
        [HttpGet]
        public async Task<IActionResult> Index(string guid)
        {
            // Указанный файл отсутствует в базе данных
            if (!dbContext.AttachedFiles.Any(s => s.Guid == guid)) //b = false
                return NotFound("File not found");

            var fileModel = dbContext.AttachedFiles.First(s => s.Guid == guid);

            fileModel.RequestedTimes++;
            await dbContext.SaveChangesAsync();

            var path = Path.Combine(Directory.GetCurrentDirectory(), "attached-files", guid);
            if (!System.IO.File.Exists(path))
            {
                //Файл есть в базе данных, но отсутствует на сервере
                //logger.LogError("File not found on server: {path}", path);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Requested file not found on server. This is an internal server error.");
            }
            var fileStream = System.IO.File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var fileDownloadName = fileModel.FileName;
            var contentType = MimeTypeMap.GetMimeType(Path.GetExtension(fileDownloadName));
            if (contentType == "application/octet-stream")
                return File(fileStream, contentType, fileDownloadName);
            return File(fileStream, contentType);
        }
    }
}
