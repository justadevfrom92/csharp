using System.ComponentModel.DataAnnotations;

namespace MvcFileUploadPortal.Validation;

/// <summary>
/// Defence in depth: the extension is chosen by the client and is trivially spoofed
/// (rename malware.exe to cute-cat.png). This attribute reads the first bytes of the stream
/// and checks the "magic number" matches the claimed extension.
/// Extensions without a known signature (e.g. .txt, .csv) are accepted as-is.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class ValidateFileContentMatchesExtensionSignatureAttribute : ValidationAttribute
{
    private static readonly Dictionary<string, byte[][]> KnownFileSignaturesByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
        [".jpg"] = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        [".gif"] = [[0x47, 0x49, 0x46, 0x38, 0x37, 0x61], [0x47, 0x49, 0x46, 0x38, 0x39, 0x61]],
        [".webp"] = [[0x52, 0x49, 0x46, 0x46]],
        [".pdf"] = [[0x25, 0x50, 0x44, 0x46, 0x2D]],
        [".docx"] = [[0x50, 0x4B, 0x03, 0x04]],
        [".xlsx"] = [[0x50, 0x4B, 0x03, 0x04]],
    };

    private static readonly int LongestSignatureLength =
        KnownFileSignaturesByExtension.Values.SelectMany(signatures => signatures).Max(signature => signature.Length);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        foreach (IFormFile uploadedFile in UploadedFileEnumerator.EnumerateUploadedFiles(value))
        {
            string extension = Path.GetExtension(uploadedFile.FileName);
            if (!KnownFileSignaturesByExtension.TryGetValue(extension, out byte[][]? acceptableSignatures))
            {
                continue;
            }

            Span<byte> headerBuffer = stackalloc byte[LongestSignatureLength];
            int bytesRead;
            using (Stream uploadedFileStream = uploadedFile.OpenReadStream())
            {
                bytesRead = uploadedFileStream.ReadAtLeast(headerBuffer, headerBuffer.Length, throwOnEndOfStream: false);
            }

            ReadOnlySpan<byte> fileHeader = headerBuffer[..bytesRead];
            bool matchesAnySignature = false;
            foreach (byte[] acceptableSignature in acceptableSignatures)
            {
                if (fileHeader.StartsWith(acceptableSignature))
                {
                    matchesAnySignature = true;
                    break;
                }
            }

            if (!matchesAnySignature)
            {
                return new ValidationResult(
                    ErrorMessage ?? $"'{uploadedFile.FileName}' does not contain valid {extension.TrimStart('.').ToUpperInvariant()} data.",
                    validationContext.MemberName is null ? null : [validationContext.MemberName]);
            }
        }
        return ValidationResult.Success;
    }
}
