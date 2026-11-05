using System.ComponentModel.DataAnnotations;
using MvcFileUploadPortal.Validation;

namespace MvcFileUploadPortal.Models;

/// <summary>Upload limits in one place; attributes require compile-time constants.</summary>
public static class UploadSizeLimits
{
    public const long MaximumAvatarImageSizeInBytes = 2 * 1024 * 1024;        // 2 MB
    public const long MaximumSupportingDocumentSizeInBytes = 5 * 1024 * 1024; // 5 MB
    public const long MaximumMultipartRequestBodyInBytes = 30 * 1024 * 1024;  // 30 MB (several documents)

    /// <summary>
    /// Transport-level ceiling for the avatar POST. Deliberately larger than the 2 MB validation limit so a
    /// 3 MB image reaches MaximumFileSizeValidationAttribute and gets a friendly message instead of a raw 413;
    /// anything above this ceiling is cut off by Kestrel before it is buffered.
    /// </summary>
    public const long MaximumAvatarRequestBodyInBytes = 10 * 1024 * 1024;     // 10 MB
}

public sealed class ProfileAvatarUploadViewModel
{
    [Required(ErrorMessage = "Choose an image to upload.")]
    [MaximumFileSizeValidation(UploadSizeLimits.MaximumAvatarImageSizeInBytes)]
    [ValidatePermittedFileExtension(".png", ".jpg", ".jpeg", ".gif", ".webp")]
    [ValidateFileContentMatchesExtensionSignature]
    [Display(Name = "Avatar image")]
    public IFormFile? AvatarImageFile { get; set; }
}

public sealed class SupportingDocumentUploadViewModel
{
    [Required(ErrorMessage = "Choose at least one document.")]
    [MinLength(1, ErrorMessage = "Choose at least one document.")]
    [MaxLength(5, ErrorMessage = "Upload at most 5 documents at a time.")]
    [MaximumFileSizeValidation(UploadSizeLimits.MaximumSupportingDocumentSizeInBytes)]
    [ValidatePermittedFileExtension(".pdf", ".docx", ".xlsx", ".txt", ".csv")]
    [ValidateFileContentMatchesExtensionSignature]
    [Display(Name = "Documents")]
    public List<IFormFile> DocumentFiles { get; set; } = [];

    [StringLength(200)]
    [Display(Name = "Notes (optional)")]
    public string? UploadNotes { get; set; }
}

public sealed record StoredUploadedFileDescriptor(
    string OriginalFileName,
    string StoredFileName,
    string PublicRelativeUrl,
    long FileSizeInBytes,
    DateTimeOffset UploadedAt);

public sealed record SupportingDocumentListPageViewModel(
    SupportingDocumentUploadViewModel UploadForm,
    IReadOnlyList<StoredUploadedFileDescriptor> StoredDocuments);
