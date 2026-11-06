using System.ComponentModel.DataAnnotations;

namespace FileUploadPortal.Models.ValidationAttributes;

// Usage: [MaximumFileSizeValidation(2)]  -> file must be 2 MB or smaller.
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
        return ValidationResult.Success; // no file is fine here; use [Required] if a file must be sent
    }
}
