using Microsoft.AspNetCore.Mvc;
using MvcFileUploadPortal.Models;
using MvcFileUploadPortal.Services;

namespace MvcFileUploadPortal.Controllers;

/// <summary>A second controller reusing the SAME validation attributes with different limits.</summary>
[Route("documents")]
public sealed class SupportingDocumentsController(IUploadedFileStorageService uploadedFileStorageService) : Controller
{
    [HttpGet("")]
    public IActionResult Index() =>
        View(new SupportingDocumentListPageViewModel(new SupportingDocumentUploadViewModel(),
            uploadedFileStorageService.ListStoredFiles(UploadStorageCategory.Documents)));

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(UploadSizeLimits.MaximumMultipartRequestBodyInBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadSizeLimits.MaximumMultipartRequestBodyInBytes)]
    public async Task<IActionResult> Index(SupportingDocumentUploadViewModel documentUpload, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(new SupportingDocumentListPageViewModel(documentUpload,
                uploadedFileStorageService.ListStoredFiles(UploadStorageCategory.Documents)));
        }

        foreach (IFormFile documentFile in documentUpload.DocumentFiles)
        {
            await uploadedFileStorageService.SaveUploadedFileAsync(documentFile, UploadStorageCategory.Documents, cancellationToken);
        }
        TempData["StatusAlertText"] = $"{documentUpload.DocumentFiles.Count} document(s) uploaded.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{storedFileName}/delete")]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(string storedFileName)
    {
        bool wasDeleted = uploadedFileStorageService.TryDeleteStoredFile(UploadStorageCategory.Documents, storedFileName);
        TempData["StatusAlertText"] = wasDeleted ? "Document deleted." : "Document not found.";
        return RedirectToAction(nameof(Index));
    }
}
