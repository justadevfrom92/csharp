using System.ComponentModel.DataAnnotations;

namespace FileUploadPortal.Models;

public class DocumentUploadForm
{
    [Required(ErrorMessage = "Choose a document.")]
    [MaximumFileSizeValidation(5)]
    [ValidatePermittedFileExtension(".pdf", ".txt")]
    public IFormFile? Document { get; set; }

    [StringLength(100)]
    public string? Note { get; set; }
}
