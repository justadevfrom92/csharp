using System.ComponentModel.DataAnnotations;

namespace MvcFileUploadPortal.Validation;

/// <summary>
/// Allows only an explicit list of file extensions (allow-list, never a deny-list).
/// Comparison is case-insensitive; ".JPG" and ".jpg" are equivalent.
/// Usage: [ValidatePermittedFileExtension(".png", ".jpg", ".jpeg")]
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class ValidatePermittedFileExtensionAttribute : ValidationAttribute
{
    private readonly HashSet<string> _permittedFileExtensions;

    public IReadOnlyCollection<string> PermittedFileExtensions => _permittedFileExtensions;

    public ValidatePermittedFileExtensionAttribute(params string[] permittedFileExtensions)
    {
        if (permittedFileExtensions.Length == 0)
        {
            throw new ArgumentException("At least one permitted extension is required.", nameof(permittedFileExtensions));
        }
        _permittedFileExtensions = permittedFileExtensions
            .Select(extension => extension.StartsWith('.') ? extension : "." + extension)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        foreach (IFormFile uploadedFile in UploadedFileEnumerator.EnumerateUploadedFiles(value))
        {
            // Path.GetFileName strips any client-supplied directory parts ("..\\..\\evil.png").
            string uploadedFileExtension = Path.GetExtension(Path.GetFileName(uploadedFile.FileName));
            if (!_permittedFileExtensions.Contains(uploadedFileExtension))
            {
                string displayedExtension = string.IsNullOrEmpty(uploadedFileExtension) ? "(none)" : uploadedFileExtension;
                return new ValidationResult(
                    ErrorMessage ?? $"File type {displayedExtension} is not allowed. Permitted types: {string.Join(", ", _permittedFileExtensions.Order())}.",
                    validationContext.MemberName is null ? null : [validationContext.MemberName]);
            }
        }
        return ValidationResult.Success;
    }
}
