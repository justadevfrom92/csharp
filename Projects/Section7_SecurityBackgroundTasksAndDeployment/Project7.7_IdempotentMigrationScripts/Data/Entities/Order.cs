namespace IdempotentMigrationScripts.Data.Entities;

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public decimal Total { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
