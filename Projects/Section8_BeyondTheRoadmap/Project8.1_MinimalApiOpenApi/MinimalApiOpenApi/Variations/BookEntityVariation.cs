namespace MinimalApiOpenApi.Variations;

// An ENTITY: a POCO (plain class, no base class, no attributes) shaped like the DATABASE row.
// It holds fields the outside world must never see or set: what we paid, who added it.
// A mutable class, because EF Core loads it and changes it in place (see 6.x).
public class BookEntityVariation
{
    public int Id { get; set; }

    public string Title { get; set; } = "";

    public string Author { get; set; } = "";

    public int Year { get; set; }

    public decimal CostPrice { get; set; }

    public string AddedBy { get; set; } = "";
}
