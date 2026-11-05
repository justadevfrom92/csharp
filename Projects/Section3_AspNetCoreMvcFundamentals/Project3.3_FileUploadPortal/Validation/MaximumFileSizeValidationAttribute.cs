using System.ComponentModel.DataAnnotations;

namespace MvcFileUploadPortal.Validation;

/// <summary>
/// Rejects any uploaded file larger than the configured byte limit. Also rejects empty (0-byte)
/// files, which are never a legitimate avatar or document.
/// Usage: [MaximumFileSizeValidation(2 * 1024 * 1024)]
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class MaximumFileSizeValidationAttribute : ValidationAttribute
{
    public long MaximumFileSizeInBytes { get; }

    public MaximumFileSizeValidationAttribute(long maximumFileSizeInBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumFileSizeInBytes);
        MaximumFileSizeInBytes = maximumFileSizeInBytes;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        foreach (IFormFile uploadedFile in UploadedFileEnumerator.EnumerateUploadedFiles(value))
        {
            if (uploadedFile.Length == 0)
            {
                return new ValidationResult($"'{uploadedFile.FileName}' is empty.", MemberNameList(validationContext));
            }
            if (uploadedFile.Length > MaximumFileSizeInBytes)
            {
                return new ValidationResult(
                    ErrorMessage ?? $"'{uploadedFile.FileName}' is {FormatByteCount(uploadedFile.Length)}; the maximum allowed size is {FormatByteCount(MaximumFileSizeInBytes)}.",
                    MemberNameList(validationContext));
            }
        }
        return ValidationResult.Success;
    }

    internal static string FormatByteCount(long byteCount) => byteCount switch
    {
        >= 1024 * 1024 => $"{byteCount / 1024d / 1024d:0.##} MB",
        >= 1024 => $"{byteCount / 1024d:0.#} KB",
        _ => $"{byteCount} bytes"
    };

    private static string[]? MemberNameList(ValidationContext validationContext) =>
        validationContext.MemberName is null ? null : [validationContext.MemberName];
}
