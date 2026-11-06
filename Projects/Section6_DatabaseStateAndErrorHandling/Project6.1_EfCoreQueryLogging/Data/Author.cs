namespace EfCoreQueryLogging.Data;

// Author 1 --- 1 AuthorProfile   (one-to-one)
// Author 1 --- N Book            (one-to-many)
// Book   N --- N Tag             (many-to-many, EF creates the join table "BookTag")

public class Author
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public AuthorProfile? Profile { get; set; }
    public List<Book> Books { get; set; } = new();
}
