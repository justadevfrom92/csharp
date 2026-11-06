namespace IdempotentMigrationScripts.Models;

public class OrderModel
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public CustomerModel Customer { get; set; } = null!;
    public decimal Total { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
