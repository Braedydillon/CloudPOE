using IncredibleComponentsPoe.Models;
using IncredibleComponentsPoe.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace IncredibleComponentsPoe.Controllers
{
    [Authorize]
    public class FileController : Controller
    {
        private readonly AzureFileShareServices _fileShareService;
        private readonly string _directoryName = "uploads"; // Azure directory name

        public FileController(AzureFileShareServices fileShareService)
        {
            _fileShareService = fileShareService;
        }

        // GET: /File
        public async Task<IActionResult> Index()
        {
            var files = await _fileShareService.ListFilesAsync(_directoryName);
            return View(files); // <-- pass model here
        }

        // POST: /File/UploadFile
        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                await _fileShareService.UploadFileAsync(_directoryName, file);
                TempData["Message"] = $"File '{file.FileName}' uploaded successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /File/DownloadFile?fileName=example.txt
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return BadRequest();

            var stream = await _fileShareService.DownloadFileAsync(_directoryName, fileName);
            if (stream == null)
                return NotFound();

            return File(stream, "application/octet-stream", fileName);
        }

        // GET: /File/DeleteFile?fileName=example.txt
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return BadRequest();

            await _fileShareService.DeleteFileAsync(_directoryName, fileName);
            TempData["Message"] = $"File '{fileName}' deleted successfully!";

            return RedirectToAction(nameof(Index));
        }
    }
}
