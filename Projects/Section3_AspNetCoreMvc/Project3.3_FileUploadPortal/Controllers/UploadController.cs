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

    // GET /  (or /Upload/Index)
    public IActionResult Index()
    {
        List<string> savedFileNames = Directory.GetFiles(uploadFolder)
            .Select(fullPath => Path.GetFileName(fullPath))
            .Where(fileName => fileName != ".gitkeep")
            .ToList();

        // View(savedFileNames) renders Views/Upload/Index.cshtml and hands it this list.
        // Inside that file, "@model List<string>" says what type it receives, and "Model" is the list.
        return View(savedFileNames);
    }

    // GET /Upload/Avatar - show the empty form.
    // AvatarUploadViewModel is defined in Models/AvatarUploadViewModel.cs.
    // View(...) renders Views/Upload/Avatar.cshtml, whose first line "@model AvatarUploadViewModel"
    // says it expects this type. The asp-for="Avatar" input in that file is tied to its Avatar property.
    [HttpGet]
    public IActionResult Avatar()
    {
        return View(new AvatarUploadViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // POST /Upload/Avatar - the form was submitted.
    // We never write "new AvatarUploadViewModel()" here: MODEL BINDING does it. MVC sees the parameter type,
    // creates an AvatarUploadViewModel, and fills its Avatar property from the uploaded file named "Avatar"
    // (that name comes from asp-for="Avatar" in the view). It also runs the validation attributes on the
    // class and stores the result in ModelState.
    public async Task<IActionResult> Avatar(AvatarUploadViewModel form)
    {
        if (!ModelState.IsValid)
        {
            // Show Avatar.cshtml again with the SAME object, so asp-validation-for can show the errors.
            return View(form);
        }

        string savedName = await SaveFileAsync(form.Avatar!);
        // TempData survives ONE redirect (ViewBag would be lost). Read in Views/Shared/_Layout.cshtml.
        TempData["Message"] = $"Avatar saved as {savedName}";
        return RedirectToAction(nameof(Index));
    }

    // GET /Upload/Document - same pattern as Avatar. DocumentUploadViewModel is in Models/DocumentUploadViewModel.cs.
    [HttpGet]
    public IActionResult Document()
    {
        return View(new DocumentUploadViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // POST /Upload/Document - model binding creates DocumentUploadViewModel and fills Document + Note.
    public async Task<IActionResult> Document(DocumentUploadViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        string savedName = await SaveFileAsync(form.Document!);
        // Read in Views/Shared/_Layout.cshtml after the redirect.
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
