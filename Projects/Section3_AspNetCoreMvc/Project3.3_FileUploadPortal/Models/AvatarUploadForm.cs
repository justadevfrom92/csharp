using System.ComponentModel.DataAnnotations;

namespace FileUploadPortal.Models;

public class AvatarUploadForm
{
    [Required(ErrorMessage = "Choose an image.")]
    [MaximumFileSizeValidation(1)]
    [ValidatePermittedFileExtension(".jpg", ".jpeg", ".png")]
    public IFormFile? Avatar { get; set; }
}
