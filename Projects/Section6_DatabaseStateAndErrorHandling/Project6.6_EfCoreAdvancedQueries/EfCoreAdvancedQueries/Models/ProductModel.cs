namespace EfCoreAdvancedQueries.Models;

public class ProductModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";

    // Money as WHOLE CENTS (2599 = 25.99). SQLite has no exact decimal type; integer cents stay exact,
    // and SQL can do arithmetic on them (needed for the bulk price update).
    public long PriceInCents { get; set; }

    public int Stock { get; set; }
    public bool IsDiscontinued { get; set; }

    // CONCURRENCY TOKEN: EF adds "WHERE Version = <the value I read>" to every UPDATE/DELETE of this row.
    // If someone else changed the row (and so the Version) in the meantime, 0 rows match and EF throws
    // DbUpdateConcurrencyException instead of silently overwriting their change.
    public int Version { get; set; }
}
