using Microsoft.AspNetCore.Mvc;
using MvcFileUploadPortal.Models;
using MvcFileUploadPortal.Services;

namespace MvcFileUploadPortal.Controllers;

/// <summary>
/// Note what is NOT here: no size checks, no extension checks, no signature checks.
/// Those are declared once on the view model via attributes; this action only asks ModelState.
/// </summary>
[Route("profile/avatar")]
public sealed class ProfileAvatarController(IUploadedFileStorageService uploadedFileStorageService) : Controller
{
    private const string CurrentAvatarUrlSessionlessKey = "CurrentAvatarUrl";

    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData[CurrentAvatarUrlSessionlessKey] = uploadedFileStorageService.ListStoredFiles(UploadStorageCategory.Avatars).FirstOrDefault()?.PublicRelativeUrl;
        return View(new ProfileAvatarUploadViewModel());
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(UploadSizeLimits.MaximumAvatarRequestBodyInBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadSizeLimits.MaximumAvatarRequestBodyInBytes)]
    public async Task<IActionResult> Index(ProfileAvatarUploadViewModel avatarUpload, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ViewData[CurrentAvatarUrlSessionlessKey] = uploadedFileStorageService.ListStoredFiles(UploadStorageCategory.Avatars).FirstOrDefault()?.PublicRelativeUrl;
            return View(avatarUpload);
        }

        StoredUploadedFileDescriptor storedAvatar = await uploadedFileStorageService.SaveUploadedFileAsync(
            avatarUpload.AvatarImageFile!, UploadStorageCategory.Avatars, cancellationToken);
        TempData["StatusAlertText"] = $"Avatar '{storedAvatar.OriginalFileName}' uploaded.";
        return RedirectToAction(nameof(Index));
    }
}
