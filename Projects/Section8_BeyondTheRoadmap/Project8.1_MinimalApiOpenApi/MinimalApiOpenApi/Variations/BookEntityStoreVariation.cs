namespace MinimalApiOpenApi.Variations;

// In-memory "table" of entities for the DTO examples (like the original BookStore, but full rows).
public class BookEntityStoreVariation
{
    private readonly List<BookEntityVariation> books =
    [
        new BookEntityVariation { Id = 1, Title = "Dune", Author = "Frank Herbert", Year = 1965, CostPrice = 4.20m, AddedBy = "admin" },
    ];

    private readonly Lock gate = new();

    public BookEntityVariation? FindById(int id)
    {
        lock (gate)
        {
            return books.FirstOrDefault(book => book.Id == id);
        }
    }

    public BookEntityVariation Add(BookEntityVariation book)
    {
        lock (gate)
        {
            book.Id = books.Max(existing => existing.Id) + 1;
            books.Add(book);
            return book;
        }
    }
}
