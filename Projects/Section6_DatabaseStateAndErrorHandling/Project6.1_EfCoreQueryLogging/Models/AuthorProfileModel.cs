namespace EfCoreQueryLogging.Models;

public class AuthorProfileModel
{
    public int Id { get; set; }
    public string Biography { get; set; } = "";
    public int AuthorId { get; set; }
    public AuthorModel Author { get; set; } = null!;
}
