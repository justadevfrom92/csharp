using System.ComponentModel.DataAnnotations;
using FileUploadPortal.Models.ValidationAttributes;

namespace FileUploadPortal.Models;

// What the "Upload document" form posts.
public class DocumentUploadViewModel
{
    [Required(ErrorMessage = "Choose a document.")]
    [MaximumFileSizeValidation(5)]
    [ValidatePermittedFileExtension(".pdf", ".txt")]
    public IFormFile? Document { get; set; }

    [StringLength(100)]
    public string? Note { get; set; }
}
