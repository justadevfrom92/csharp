using System.Collections.Immutable;
using FluentBuilderAndDsl.Models;

namespace FluentBuilderAndDsl.Variations;

// An IMMUTABLE builder: every step returns a NEW builder instead of changing "this" (OrderBuilder, the original,
// changes itself and returns this). So one half-built builder can be reused as a starting point safely:
//   var forAnn = new ImmutableOrderBuilderVariation().ForCustomer("Ann");
//   var a = forAnn.AddLine(...);  var b = forAnn.AddLine(...);   // a and b don't affect each other
public record ImmutableOrderBuilderVariation
{
    private string? customer;
    private ImmutableList<OrderLineModel> lines = ImmutableList<OrderLineModel>.Empty;

    public ImmutableOrderBuilderVariation ForCustomer(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return this with { customer = name.Trim() };
    }

    public ImmutableOrderBuilderVariation AddLine(string sku, int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be more than 0.");
        }
        return this with { lines = lines.Add(new OrderLineModel(sku, quantity, unitPrice)) };
    }

    public OrderModel Build()
    {
        if (customer == null || lines.Count == 0)
        {
            throw new InvalidOperationException("A customer and at least one line are required.");
        }
        return new OrderModel(customer, "US", lines, null);
    }
}
