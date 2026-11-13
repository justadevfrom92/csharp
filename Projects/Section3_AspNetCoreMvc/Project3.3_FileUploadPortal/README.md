# Project 3.3 — Avatar & Document Upload Portal

> New to `.cshtml` files (`@model`, `@foreach`, `ViewBag`, layouts)? Read [RAZOR_BASICS.md](../../RAZOR_BASICS.md) first.

```bash
dotnet run
```
Files are saved to `wwwroot/uploads/` with a random name and listed on the home page.

## What it covers

- Two custom validation attributes:
  - `MaximumFileSizeValidationAttribute(megabytes)`
  - `ValidatePermittedFileExtensionAttribute(".jpg", ".png", ...)`
- Two forms (`AvatarUploadForm`, `DocumentUploadForm`) that reuse them with different limits.
- `IFormFile` + `CopyToAsync` to save the stream, `UseStaticFiles` to serve the result.

## Why these choices

**Why custom attributes instead of `if` checks in the controller (DRY).**
Without them, every upload action repeats the same code: "is the file too big? is the
extension allowed? add a model error". With two upload actions that's already copied twice,
and the next upload page would copy it again. Writing each rule once as an attribute means:
- the rule lives in one place — fix a bug once, it's fixed everywhere;
- the limits are visible right on the model (`[MaximumFileSizeValidation(1)]`), not hidden in a controller;
- the controller only asks `ModelState.IsValid`, the same as for any other field.

**Why each attribute checks only one thing.** Size and extension are separate rules, so each
gets its own attribute. Forms can mix and match them, and the error message says exactly what's wrong.

**Why the attributes allow "no file".** Whether a file is *required* is a different rule,
already covered by the built-in `[Required]`. Keeping them separate avoids duplicate messages.

**Why the saved file gets a random name.** The browser sends the original file name, and a
user can fake it (e.g. `../../appsettings.json`). We only keep the extension (already checked)
and use a GUID, so a file can never overwrite another file or land outside `uploads/`.

**Limits of this demo.** The extension check trusts the file name. A real site should also
check the file's first bytes (e.g. PNG files start with `89 50 4E 47`) and maybe scan for viruses.

## Code snippets

**A custom validation attribute**, from `Attributes/MaximumFileSizeValidationAttribute.cs`

```csharp
public class MaximumFileSizeValidationAttribute : ValidationAttribute
{
    private readonly int maximumMegabytes;

    public MaximumFileSizeValidationAttribute(int maximumMegabytes)
    {
        this.maximumMegabytes = maximumMegabytes;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is IFormFile file && file.Length > maximumMegabytes * 1024L * 1024L)
        {
            return new ValidationResult($"File must be {maximumMegabytes} MB or smaller.");
        }
        return ValidationResult.Success;
    }
}
```

**A form class using the attributes**, from `Forms/AvatarUploadForm.cs`

```csharp
public class AvatarUploadForm
{
    [Required(ErrorMessage = "Choose an image.")]
    [MaximumFileSizeValidation(1)]
    [ValidatePermittedFileExtension(".jpg", ".jpeg", ".png")]
    public IFormFile? Avatar { get; set; }
}
```

**The upload form: `enctype="multipart/form-data"` is required to send files**, from `Views/Upload/Avatar.cshtml`

```cshtml
<form asp-action="Avatar" method="post" enctype="multipart/form-data">
    <input asp-for="Avatar" type="file" accept=".jpg,.jpeg,.png" />
    <span asp-validation-for="Avatar" class="error"></span>
    <button type="submit">Upload</button>
</form>
```

**Receiving the file and saving it under a safe, random name**, from `Controllers/UploadController.cs`

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Avatar(AvatarUploadForm form)
{
    if (!ModelState.IsValid)
    {
        return View(form);
    }
    string savedName = await SaveFileAsync(form.Avatar!);
    TempData["Message"] = $"Avatar saved as {savedName}";
    return RedirectToAction(nameof(Index));
}

private async Task<string> SaveFileAsync(IFormFile file)
{
    // Never trust the user's file name (it could be "../../Program.cs").
    string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    string savedName = Guid.NewGuid().ToString("N") + extension;
    await using FileStream fileStream = System.IO.File.Create(Path.Combine(uploadFolder, savedName));
    await file.CopyToAsync(fileStream);
    return savedName;
}
```

**`ViewBag` for a small extra value**

```csharp
ViewBag.SavedFiles = Directory.GetFiles(uploadFolder).Select(Path.GetFileName).ToList();
```

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| Attributes inherit `ValidationAttribute` | MVC already calls `IsValid` on every `ValidationAttribute` (polymorphism). We only override that one method. | Writing our own checking system would duplicate what MVC does. |
| `params string[] permittedExtensions` | Lets you write `[ValidatePermittedFileExtension(".jpg", ".png")]`. Attributes can only take constants and arrays. | A `List<string>` can't be passed to an attribute. |
| `int maximumMegabytes` | Readable in the attribute: `[MaximumFileSizeValidation(5)]`. | Bytes (`5242880`) are hard to read. |
| `1024L * 1024L` (`long`) | `IFormFile.Length` is a `long`. Using `L` makes the multiply `long` math. | `int` math overflows at 2,048 MB and wraps negative — the bug from 1.1. |
| `IFormFile?` on the form models | The user may submit without choosing a file. `[Required]` reports that properly. | Non-nullable would lie: the binder *can* leave it null. |
| Two form classes, not one | Avatar and document have different limits and fields. | One class can't carry two different sets of attributes on the same property. |
| `readonly string uploadFolder` | Worked out once in the constructor, never changes. | — |
| `async Task<IActionResult>` | Saving a file is disk I/O; the thread is freed while waiting. | A sync action would block a thread per upload. |
| `Guid` for saved names | Unique without checking the folder; can't contain `../`. | User's file name can be faked and can clash. |
| `await using FileStream` | Closes the file even if copying throws. | Forgetting to close leaves the file locked. |
