namespace MvcFileUploadPortal.Validation;

/// <summary>
/// Normalises the value a file validation attribute receives: a single IFormFile, a collection
/// of IFormFile, or null. Every attribute shares this, so none of them repeats the type checks.
/// </summary>
internal static class UploadedFileEnumerator
{
    public static IEnumerable<IFormFile> EnumerateUploadedFiles(object? validatedValue) => validatedValue switch
    {
        null => [],
        IFormFile singleUploadedFile => [singleUploadedFile],
        IEnumerable<IFormFile> multipleUploadedFiles => multipleUploadedFiles,
        _ => throw new InvalidOperationException(
            $"File validation attributes can only be applied to IFormFile or IEnumerable<IFormFile>, not {validatedValue.GetType().Name}.")
    };
}
