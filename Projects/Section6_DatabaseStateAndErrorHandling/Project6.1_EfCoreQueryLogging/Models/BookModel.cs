namespace EfCoreQueryLogging.Models;

public class BookModel
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public int PublishedYear { get; set; }
    public int AuthorId { get; set; }
    public AuthorModel Author { get; set; } = null!;
    public List<TagModel> Tags { get; set; } = new();
}
