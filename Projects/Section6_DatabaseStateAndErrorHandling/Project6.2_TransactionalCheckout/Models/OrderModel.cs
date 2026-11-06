namespace TransactionalCheckout.Models;

public class OrderModel
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public decimal Total { get; set; }
    public List<OrderLineModel> Lines { get; set; } = new();
}
