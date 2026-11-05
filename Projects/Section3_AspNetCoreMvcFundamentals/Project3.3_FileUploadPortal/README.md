# Mini-Project 3.3 — Profile Avatar & Document Upload Portal

## Run

```bash
dotnet run      # http://localhost:5033   (/profile/avatar, /documents)
```

Uploaded files land in `wwwroot/uploads/avatars/` and `wwwroot/uploads/documents/` (git-ignored).

## Validation attributes

| Attribute | Rule | Applied to |
|---|---|---|
| `MaximumFileSizeValidationAttribute(long maximumFileSizeInBytes)` | Rejects files that are empty or over the limit; message shows actual vs allowed size | Avatar (2 MB), Documents (5 MB each) |
| `ValidatePermittedFileExtensionAttribute(params string[])` | Case-insensitive **allow-list** of extensions | Avatar (images), Documents (pdf/docx/xlsx/txt/csv) |
| `ValidateFileContentMatchesExtensionSignatureAttribute` | Reads the first bytes and checks the magic number matches the claimed extension | Both |
| `UploadedFileEnumerator` (internal helper) | Lets every attribute accept `IFormFile` **or** `IEnumerable<IFormFile>` | Shared by all three |

Smoke-test results (curl, multipart POST with anti-forgery token):

| Upload | Result |
|---|---|
| `fake.png` (text renamed to .png) | "'fake.png' does not contain valid PNG data." |
| `notes.exe` | "File type .exe is not allowed. Permitted types: .gif, .jpeg, .jpg, .png, .webp." |
| 2.86 MB PNG | "'big.png' is 2.86 MB; the maximum allowed size is 2 MB." |
| no file | "Choose an image to upload." |
| valid PNG named `../../evil.png` | Stored as `/uploads/avatars/<guid>.png`, served with `X-Content-Type-Options: nosniff` |
| `doc.pdf` + `data.csv` | Both stored; PDFs served with `Content-Disposition: attachment` |
| `doc.pdf` + `notes.exe` in one POST | Whole batch rejected, nothing written |
| 12 MB body | Rejected at the transport layer (400) before buffering |

## Why the validation attributes are structured this way: DRY across controllers (the required write-up)

**The problem they remove.** Without attributes, every upload action would have to repeat:

```csharp
if (file == null || file.Length == 0) ModelState.AddModelError(...);
if (file.Length > 2 * 1024 * 1024) ModelState.AddModelError(...);
var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
if (!new[] { ".png", ".jpg" }.Contains(ext)) ModelState.AddModelError(...);
// ... and for multi-file inputs, wrap it all in a loop
```

Two controllers means two copies; ten means ten. The copies drift: one forgets case-insensitivity,
another forgets the 0-byte check, and a third uses a deny-list.

**How the structure enforces DRY:**

1. **One rule per attribute (single responsibility).** Size, extension and content signature are
   separate classes. A view model composes exactly the rules it needs, and each rule can be
   tested and changed independently.
2. **Rules are declared on the view model, not the controller.** `ProfileAvatarController` and
   `SupportingDocumentsController` contain *zero* file checks; they only ask
   `ModelState.IsValid`. Adding a third upload screen means writing a view model with attributes,
   not copying code.
3. **Parameterised, not hard-coded.** The same `MaximumFileSizeValidationAttribute` enforces 2 MB on
   avatars and 5 MB on documents. Limits are named constants in `UploadSizeLimits` (attributes
   require compile-time constants), so the number appears once.
4. **Single and multiple files share one code path.** `UploadedFileEnumerator` turns `IFormFile`,
   `IEnumerable<IFormFile>` or `null` into a sequence. Every attribute loops over that sequence, so
   none of them repeats type checks, and an unsupported property type fails loudly at first use.
5. **`null` is not the attributes' job.** They succeed on `null`, and `[Required]` owns "missing".
   This is the DataAnnotations convention, and it keeps responsibilities from overlapping
   (otherwise "no file" would produce three error messages).
6. **They plug into the framework pipeline.** Because they derive from `ValidationAttribute`,
   MVC runs them automatically during model binding. Error messages land in `ModelState` under
   the correct property name (`validationContext.MemberName`), so `asp-validation-for` shows them next to the right input.
7. **Precise naming.** `ValidatePermittedFileExtensionAttribute` says it validates, what it
   validates, and that it is an allow-list ("permitted"). `MaximumFileSizeValidationAttribute`
   names the bound and the unit of work. A vague name like `AllowedExtension` or `FileCheck` would hide the semantics.

## Security decisions in storage (`SecureUploadedFileStorageService`)

* **Never trust the client file name.** The stored name is a random GUID plus the normalised extension.
  The original name is kept in a sidecar file only for display, and `Path.GetFileName`
  strips any `../` the client sent.
* **Path traversal guard.** Every path is canonicalised with `Path.GetFullPath` and must start
  with the category folder; otherwise `UnauthorizedAccessException`.
* **`FileMode.CreateNew`** never overwrites an existing file.
* **Streamed copy** (`CopyToAsync` with an 80 KB buffer) rather than reading the file into a byte array.
* **Defence in depth.** The extension allow-list is backed by a magic-number check, because renaming
  `malware.exe` to `cat.png` is trivial.
* **Two size layers.** Kestrel/`[RequestSizeLimit]` (10 MB for avatars) is a hard transport ceiling that
  stops abuse before buffering. The attribute (2 MB) gives a friendly per-file message. The transport
  limit is deliberately larger than the validation limit. The first version used 2 MB + 64 KB, and a
  3 MB upload got a raw 413 instead of a helpful message.
* **Serving.** `ServeUnknownFileTypes = false` (sidecars are never served), `nosniff` on all
  static responses, and `Content-Disposition: attachment` for documents so they never render inline
  on the site's origin. In production, store uploads **outside** `wwwroot` (or in blob storage) and serve
  them through an authorised action; `wwwroot/uploads` is used here because the brief asks for it.
