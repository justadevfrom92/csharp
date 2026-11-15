using Microsoft.AspNetCore.Mvc;
using FileUploadPortal.Forms;

namespace FileUploadPortal.Variations;

// Receiving uploaded files, five ways. Nothing here is SAVED - each action only reports what it received,
// so you can compare them safely with curl (see README). [IgnoreAntiforgeryToken] only so curl can post;
// real upload forms keep the token (see UploadController).
[Route("variations/upload")]
[IgnoreAntiforgeryToken]
public class UploadVariation : Controller
{
    private static readonly byte[] pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] jpegSignature = [0xFF, 0xD8, 0xFF];

    // ORIGINAL style: a form class with validation attributes (size + extension).
    [HttpPost("form-class")]
    public IActionResult WithFormClass(AvatarUploadForm form)
    {
        return Content(Report(form.Avatar));
    }

    // An IFormFile PARAMETER: no form class, so no attributes run - every check must be written here.
    [HttpPost("parameter")]
    public IActionResult WithParameter(IFormFile? avatar)
    {
        if (avatar == null || avatar.Length == 0)
        {
            ModelState.AddModelError(nameof(avatar), "Choose a file.");
        }
        return Content(Report(avatar));
    }

    // SEVERAL files under one name (<input type="file" name="files" multiple>).
    [HttpPost("multiple")]
    public IActionResult WithList(List<IFormFile> files)
    {
        return Content($"{files.Count} file(s): {string.Join(", ", files.Select(file => $"{file.FileName} ({file.Length} bytes)"))}");
    }

    // Request.Form.Files: no model binding at all - every file in the request, whatever its field name.
    [HttpPost("request-form")]
    public async Task<IActionResult> WithRequestForm()
    {
        IFormCollection form = await Request.ReadFormAsync();
        return Content($"{form.Files.Count} file(s) in fields: {string.Join(", ", form.Files.Select(file => file.Name))}");
    }

    // EXTENSION check vs CONTENT check. The extension is just part of the name - anyone can rename virus.exe to
    // photo.png. Real image files START with fixed bytes (a "signature" / "magic number"); checking those is stronger.
    [HttpPost("signature")]
    public async Task<IActionResult> CheckSignature(IFormFile? avatar)
    {
        if (avatar == null)
        {
            return BadRequest("Choose a file.");
        }
        bool extensionLooksOk = new[] { ".png", ".jpg", ".jpeg" }.Contains(Path.GetExtension(avatar.FileName).ToLowerInvariant());
        byte[] start = new byte[8];
        await using Stream stream = avatar.OpenReadStream();
        int read = await stream.ReadAsync(start);
        bool contentIsImage = StartsWith(start, read, pngSignature) || StartsWith(start, read, jpegSignature);
        return Content($"extension says image: {extensionLooksOk}, content says image: {contentIsImage}");
    }

    // [RequestSizeLimit]: the server stops READING the body once it passes the limit, so a huge upload can't fill
    // memory or disk. The action still runs, but the file couldn't be bound: avatar is NULL and ModelState has the
    // reason. (Forgetting that null check was a real bug while writing this: avatar.Length crashed with a 500.)
    // The [MaximumFileSizeValidation] attribute (original) only checks AFTER the whole file has been received.
    [HttpPost("size-limit")]
    [RequestSizeLimit(2048)]
    public IActionResult WithRequestSizeLimit(IFormFile? avatar)
    {
        if (avatar == null)
        {
            return BadRequest(Report(avatar));
        }
        return Content($"accepted {avatar.Length} bytes");
    }

    // Which name would each saving strategy use for this upload? (Computed only - nothing is written.)
    [HttpGet("save-names")]
    public IActionResult SaveNames(string fileName)
    {
        string folder = "/app/wwwroot/uploads";
        string original = Guid.NewGuid().ToString("N") + Path.GetExtension(fileName).ToLowerInvariant();  // ORIGINAL
        string fileNameOnly = Path.GetFileName(fileName);                                                  // strips folders, keeps the user's name
        string fullPathRaw = Path.GetFullPath(Path.Combine(folder, fileName));                             // trusts the name completely
        return Content($"guid: {Path.Combine(folder, original)}\nGetFileName: {Path.Combine(folder, fileNameOnly)}\nraw: {fullPathRaw}");
    }

    private static bool StartsWith(byte[] buffer, int length, byte[] signature)
    {
        return length >= signature.Length && buffer.AsSpan(0, signature.Length).SequenceEqual(signature);
    }

    private string Report(IFormFile? file)
    {
        string errors = string.Join(" | ", ModelState.Values.SelectMany(entry => entry.Errors).Select(error => error.ErrorMessage));
        return $"file={file?.FileName ?? "none"} valid={ModelState.IsValid} {errors}".Trim();
    }
}
