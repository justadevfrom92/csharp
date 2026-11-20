namespace AsyncStreams.Models;

// A database row (EF Core entity): a plain mutable class.
public class OrderModel
{
    public int Id { get; set; }

    public string Customer { get; set; } = "";

    public decimal Total { get; set; }
}
