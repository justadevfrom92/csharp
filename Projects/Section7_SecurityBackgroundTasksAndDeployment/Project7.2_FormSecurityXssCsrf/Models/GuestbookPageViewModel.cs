using System.ComponentModel.DataAnnotations;
using FormSecurityXssCsrf.Data.Entities;

namespace FormSecurityXssCsrf.Models;

// The guestbook page: the "post a message" form fields + the entries to show.
public class GuestbookPageViewModel
{
    // ----- the form (posted) -----
    [Required]
    [StringLength(40)]
    public string Name { get; set; } = "";

    [Required]
    [StringLength(500)]
    public string Message { get; set; } = "";

    [Url]
    public string? Website { get; set; }

    // ----- shown on the page (filled by the controller, not posted) -----
    public List<GuestbookEntry> Entries { get; set; } = new();
}
