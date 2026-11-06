using System.ComponentModel.DataAnnotations;
using FileUploadPortal.Models.ValidationAttributes;

namespace FileUploadPortal.Models;

// What the "Upload avatar" form posts. Model binding fills it; the attributes check the file.
public class AvatarUploadViewModel
{
    [Required(ErrorMessage = "Choose an image.")]
    [MaximumFileSizeValidation(1)]
    [ValidatePermittedFileExtension(".jpg", ".jpeg", ".png")]
    public IFormFile? Avatar { get; set; }
}
