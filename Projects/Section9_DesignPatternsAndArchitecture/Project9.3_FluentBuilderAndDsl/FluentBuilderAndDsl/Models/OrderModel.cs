namespace FluentBuilderAndDsl.Models;

// Finished order: can't be changed after OrderBuilder.Build() creates it.
public record OrderModel(string Customer, string Country, IReadOnlyList<OrderLineModel> Lines, string? Note)
{
    public decimal Subtotal => Lines.Sum(line => line.UnitPrice * line.Quantity);
    public int ItemCount => Lines.Sum(line => line.Quantity);
}
