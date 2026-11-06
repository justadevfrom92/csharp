using System.ComponentModel.DataAnnotations;

namespace FormSecurityXssCsrf.Models;

public class GuestbookEntryModel
{
    [Required]
    [StringLength(40)]
    public string Name { get; set; } = "";

    [Required]
    [StringLength(500)]
    public string Message { get; set; } = "";

    [Url]
    public string? Website { get; set; }

    public DateTime PostedAtUtc { get; set; }
}
