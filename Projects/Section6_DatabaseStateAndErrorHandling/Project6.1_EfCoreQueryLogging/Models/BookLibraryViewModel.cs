using System.ComponentModel.DataAnnotations;
using EfCoreQueryLogging.Data.Entities;

namespace EfCoreQueryLogging.Models;

// The Books page: the list of books to SHOW, plus the "Add book" form fields it POSTS.
public class BookLibraryViewModel
{
    // ----- shown on the page (filled by the controller, not posted) -----
    public List<Book> Books { get; set; } = new();
    public List<Tag> AllTags { get; set; } = new();

    // ----- the "Add book" form (posted) -----
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200)]
    public string Title { get; set; } = "";

    [Range(1, 2100, ErrorMessage = "Year must be between 1 and 2100.")]
    public int PublishedYear { get; set; } = DateTime.Today.Year;

    [Required(ErrorMessage = "Author is required.")]
    [StringLength(100)]
    public string AuthorName { get; set; } = "";

    // Ids of the ticked tag checkboxes. Several checkboxes with the same name arrive as one array.
    public int[] SelectedTagIds { get; set; } = [];
}
