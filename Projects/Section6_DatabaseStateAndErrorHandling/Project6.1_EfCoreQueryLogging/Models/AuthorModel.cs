namespace EfCoreQueryLogging.Models;

// AuthorModel 1 --- 1 AuthorProfileModel   (one-to-one)
// AuthorModel 1 --- N BookModel            (one-to-many)
// BookModel   N --- N TagModel             (many-to-many, EF creates the join table "BookTag")

public class AuthorModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public AuthorProfileModel? Profile { get; set; }
    public List<BookModel> Books { get; set; } = new();
}
