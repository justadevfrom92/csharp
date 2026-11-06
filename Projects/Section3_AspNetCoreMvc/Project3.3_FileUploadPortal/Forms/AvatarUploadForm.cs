using System.ComponentModel.DataAnnotations;
using FileUploadPortal.Attributes;

namespace FileUploadPortal.Forms;

public class AvatarUploadForm
{
    [Required(ErrorMessage = "Choose an image.")]
    [MaximumFileSizeValidation(1)]
    [ValidatePermittedFileExtension(".jpg", ".jpeg", ".png")]
    public IFormFile? Avatar { get; set; }
}
