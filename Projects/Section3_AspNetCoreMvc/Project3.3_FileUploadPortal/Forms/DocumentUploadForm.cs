using System.ComponentModel.DataAnnotations;
using FileUploadPortal.Attributes;

namespace FileUploadPortal.Forms;

public class DocumentUploadForm
{
    [Required(ErrorMessage = "Choose a document.")]
    [MaximumFileSizeValidation(5)]
    [ValidatePermittedFileExtension(".pdf", ".txt")]
    public IFormFile? Document { get; set; }

    [StringLength(100)]
    public string? Note { get; set; }
}
