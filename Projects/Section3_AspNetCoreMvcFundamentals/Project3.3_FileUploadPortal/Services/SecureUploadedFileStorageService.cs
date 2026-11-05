using MvcFileUploadPortal.Models;

namespace MvcFileUploadPortal.Services;

public enum UploadStorageCategory
{
    Avatars,
    Documents
}

public interface IUploadedFileStorageService
{
    Task<StoredUploadedFileDescriptor> SaveUploadedFileAsync(IFormFile uploadedFile, UploadStorageCategory storageCategory, CancellationToken cancellationToken);
    IReadOnlyList<StoredUploadedFileDescriptor> ListStoredFiles(UploadStorageCategory storageCategory);
    bool TryDeleteStoredFile(UploadStorageCategory storageCategory, string storedFileName);
}

/// <summary>
/// Persists validated uploads under wwwroot/uploads/{category}/.
/// Security rules enforced here (independent of validation attributes, which could be bypassed
/// by a future caller that forgets ModelState.IsValid):
///  * the client file name is NEVER used as the storage path: a random GUID name is generated;
///  * the final path is canonicalised and verified to sit inside the category folder;
///  * FileMode.CreateNew guarantees an existing file is never overwritten;
///  * the stream is copied asynchronously in chunks, never buffered fully into memory.
/// </summary>
public sealed class SecureUploadedFileStorageService : IUploadedFileStorageService
{
    private readonly string _uploadsRootDirectory;
    private readonly ILogger<SecureUploadedFileStorageService> _logger;
    private readonly TimeProvider _timeProvider;

    public SecureUploadedFileStorageService(IWebHostEnvironment webHostEnvironment, ILogger<SecureUploadedFileStorageService> logger, TimeProvider timeProvider)
    {
        _uploadsRootDirectory = Path.GetFullPath(Path.Combine(webHostEnvironment.WebRootPath, "uploads"));
        _logger = logger;
        _timeProvider = timeProvider;
        foreach (UploadStorageCategory category in Enum.GetValues<UploadStorageCategory>())
        {
            Directory.CreateDirectory(GetCategoryDirectory(category));
        }
    }

    public async Task<StoredUploadedFileDescriptor> SaveUploadedFileAsync(IFormFile uploadedFile, UploadStorageCategory storageCategory, CancellationToken cancellationToken)
    {
        string safeOriginalFileName = Path.GetFileName(uploadedFile.FileName);
        string normalisedExtension = Path.GetExtension(safeOriginalFileName).ToLowerInvariant();
        string storedFileName = $"{Guid.NewGuid():N}{normalisedExtension}";
        string destinationPath = ResolvePathInsideCategoryDirectory(storageCategory, storedFileName);

        await using (var destinationStream = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                         bufferSize: 81920, useAsync: true))
        {
            await uploadedFile.CopyToAsync(destinationStream, cancellationToken);
        }

        // Keep the user's original name in a sidecar so the list page can show it, without trusting it as a path.
        await File.WriteAllTextAsync(destinationPath + ".original-name", safeOriginalFileName, cancellationToken);

        _logger.LogInformation("Stored upload {OriginalFileName} ({FileSizeInBytes} bytes) as {StoredFileName} in {StorageCategory}",
            safeOriginalFileName, uploadedFile.Length, storedFileName, storageCategory);

        return new StoredUploadedFileDescriptor(safeOriginalFileName, storedFileName,
            BuildPublicRelativeUrl(storageCategory, storedFileName), uploadedFile.Length, _timeProvider.GetUtcNow());
    }

    public IReadOnlyList<StoredUploadedFileDescriptor> ListStoredFiles(UploadStorageCategory storageCategory)
    {
        var categoryDirectory = new DirectoryInfo(GetCategoryDirectory(storageCategory));
        return categoryDirectory.EnumerateFiles()
            .Where(file => file.Name != ".gitkeep" && !file.Name.EndsWith(".original-name", StringComparison.Ordinal))
            .OrderByDescending(file => file.CreationTimeUtc)
            .Select(file =>
            {
                string sidecarPath = file.FullName + ".original-name";
                string originalFileName = File.Exists(sidecarPath) ? File.ReadAllText(sidecarPath) : file.Name;
                return new StoredUploadedFileDescriptor(originalFileName, file.Name,
                    BuildPublicRelativeUrl(storageCategory, file.Name), file.Length, new DateTimeOffset(file.CreationTimeUtc, TimeSpan.Zero));
            })
            .ToList();
    }

    public bool TryDeleteStoredFile(UploadStorageCategory storageCategory, string storedFileName)
    {
        string targetPath;
        try
        {
            targetPath = ResolvePathInsideCategoryDirectory(storageCategory, storedFileName);
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        if (!File.Exists(targetPath))
        {
            return false;
        }
        File.Delete(targetPath);
        File.Delete(targetPath + ".original-name");
        return true;
    }

    private string GetCategoryDirectory(UploadStorageCategory storageCategory) =>
        Path.Combine(_uploadsRootDirectory, storageCategory.ToString().ToLowerInvariant());

    /// <summary>Blocks path traversal: "../../appsettings.json" resolves outside the folder and is refused.</summary>
    private string ResolvePathInsideCategoryDirectory(UploadStorageCategory storageCategory, string fileName)
    {
        string categoryDirectory = GetCategoryDirectory(storageCategory) + Path.DirectorySeparatorChar;
        string candidatePath = Path.GetFullPath(Path.Combine(categoryDirectory, fileName));
        if (!candidatePath.StartsWith(categoryDirectory, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException($"Refusing to access a path outside the {storageCategory} upload folder.");
        }
        return candidatePath;
    }

    private static string BuildPublicRelativeUrl(UploadStorageCategory storageCategory, string storedFileName) =>
        $"/uploads/{storageCategory.ToString().ToLowerInvariant()}/{storedFileName}";
}
