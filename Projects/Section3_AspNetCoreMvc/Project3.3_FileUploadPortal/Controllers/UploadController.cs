using FileUploadPortal.Forms;
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
        // ViewBag = a "bag" you can put any value into in the controller, and read in the view
        // (Views/Upload/Index.cshtml reads ViewBag.SavedFiles). It only lives for this one request.
        // It is "dynamic": the compiler does NOT check the name, so a typo like ViewBag.SavedFile
        // only shows up as an error when the page runs.
        ViewBag.SavedFiles = Directory.GetFiles(uploadFolder)
            .Select(Path.GetFileName)
            .Where(name => name != ".gitkeep")
            .ToList();
        return View();
    }

    // GET /Upload/Avatar - show the empty form.
    // AvatarUploadForm is defined in Forms/AvatarUploadForm.cs.
    // View(...) renders Views/Upload/Avatar.cshtml, whose first line "@model AvatarUploadForm"
    // says it expects this type. The asp-for="Avatar" input in that file is tied to its Avatar property.
    [HttpGet]
    public IActionResult Avatar()
    {
        return View(new AvatarUploadForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // POST /Upload/Avatar - the form was submitted.
    // We never write "new AvatarUploadForm()" here: MODEL BINDING does it. MVC sees the parameter type,
    // creates an AvatarUploadForm, and fills its Avatar property from the uploaded file named "Avatar"
    // (that name comes from asp-for="Avatar" in the view). It also runs the validation attributes on the
    // class and stores the result in ModelState.
    public async Task<IActionResult> Avatar(AvatarUploadForm form)
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

    // GET /Upload/Document - same pattern as Avatar. DocumentUploadForm is in Forms/DocumentUploadForm.cs.
    [HttpGet]
    public IActionResult Document()
    {
        return View(new DocumentUploadForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // POST /Upload/Document - model binding creates DocumentUploadForm and fills Document + Note.
    public async Task<IActionResult> Document(DocumentUploadForm form)
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
