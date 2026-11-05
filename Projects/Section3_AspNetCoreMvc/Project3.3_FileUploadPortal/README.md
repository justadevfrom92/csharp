# Project 3.3 — Avatar & Document Upload Portal

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
