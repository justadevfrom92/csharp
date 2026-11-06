using System.ComponentModel.DataAnnotations;

namespace FileUploadPortal.Models.ValidationAttributes;

// Usage: [ValidatePermittedFileExtension(".jpg", ".png")]
public class ValidatePermittedFileExtensionAttribute : ValidationAttribute
{
    private readonly string[] permittedExtensions;

    public ValidatePermittedFileExtensionAttribute(params string[] permittedExtensions)
    {
        this.permittedExtensions = permittedExtensions;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IFormFile file)
        {
            return ValidationResult.Success;
        }

        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!permittedExtensions.Contains(extension))
        {
            return new ValidationResult($"Only these file types are allowed: {string.Join(", ", permittedExtensions)}");
        }
        return ValidationResult.Success;
    }
}
