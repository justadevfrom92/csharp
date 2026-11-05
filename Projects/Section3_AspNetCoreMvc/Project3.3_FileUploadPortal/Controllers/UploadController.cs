using FileUploadPortal.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileUploadPortal.Controllers;

public class UploadController : Controller
{
    private readonly string uploadFolder;

    public UploadController(IWebHostEnvironment environment)
    {
        uploadFolder = Path.Combine(environment.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadFolder);
    }

    public IActionResult Index()
    {
        ViewBag.SavedFiles = Directory.GetFiles(uploadFolder)
            .Select(Path.GetFileName)
            .Where(name => name != ".gitkeep")
            .ToList();
        return View();
    }

    [HttpGet]
    public IActionResult Avatar()
    {
        return View(new AvatarUploadForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Avatar(AvatarUploadForm form)
    {
        if (!ModelState.IsValid)
        {
            return View(form); // the attributes already checked size and extension
        }

        string savedName = await SaveFileAsync(form.Avatar!);
        TempData["Message"] = $"Avatar saved as {savedName}";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Document()
    {
        return View(new DocumentUploadForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Document(DocumentUploadForm form)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        string savedName = await SaveFileAsync(form.Document!);
        TempData["Message"] = $"Document saved as {savedName}";
        return RedirectToAction(nameof(Index));
    }

    private async Task<string> SaveFileAsync(IFormFile file)
    {
        // Never trust the user's file name (it could be "../../Program.cs").
        // Keep only the extension and make up a new random name.
        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        string savedName = Guid.NewGuid().ToString("N") + extension;
        string fullPath = Path.Combine(uploadFolder, savedName);

        await using FileStream fileStream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(fileStream);
        return savedName;
    }
}
