namespace EfCoreQueryLogging.Data.Entities;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public int PublishedYear { get; set; }
    public int AuthorId { get; set; }
    public Author Author { get; set; } = null!;
    public List<Tag> Tags { get; set; } = new();
}
