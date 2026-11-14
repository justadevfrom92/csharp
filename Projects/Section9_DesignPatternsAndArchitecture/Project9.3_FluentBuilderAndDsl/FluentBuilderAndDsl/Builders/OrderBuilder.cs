using FluentBuilderAndDsl.Models;

namespace FluentBuilderAndDsl.Builders;

// FLUENT BUILDER: builds a complicated object step by step. Every step returns "this",
// so calls can be CHAINED and read like a sentence:
//
//   OrderModel order = new OrderBuilder()
//       .ForCustomer("Ann")
//       .ShipTo("CA")
//       .AddLine("KB-01", 2, 25m)
//       .Build();
//
// Why not one constructor with 6 parameters? Optional parts (note, many lines) make that messy, and
// new OrderModel("Ann", "CA", ...) doesn't say what each value IS. Build() checks everything once, at the end.
public class OrderBuilder
{
    private readonly List<OrderLineModel> lines = new();
    private string? customer;
    private string country = "US";
    private string? note;

    public OrderBuilder ForCustomer(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        customer = name.Trim();
        return this;
    }

    public OrderBuilder ShipTo(string countryCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(countryCode);
        if (countryCode.Length != 2)
        {
            throw new ArgumentException("Country must be a 2-letter code like US.", nameof(countryCode));
        }
        country = countryCode.ToUpperInvariant();
        return this;
    }

    public OrderBuilder AddLine(string sku, int quantity, decimal unitPrice)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be more than 0.");
        }
        if (unitPrice <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Price must be more than 0.");
        }
        lines.Add(new OrderLineModel(sku, quantity, unitPrice));
        return this;
    }

    public OrderBuilder WithNote(string text)
    {
        note = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        return this;
    }

    // The rules that involve SEVERAL steps are checked here, once everything has been set.
    public OrderModel Build()
    {
        if (customer == null)
        {
            throw new InvalidOperationException("Call ForCustomer(...) before Build().");
        }
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("An order needs at least one line.");
        }
        return new OrderModel(customer, country, lines.ToList(), note); // ToList: the order gets its own copy
    }
}
