namespace EfCoreModelling.Models;

// One line of an invoice. Points at a product by its PublicId (no navigation property needed).
// Its key ("Id") and the link to its invoice ("InvoiceId") are SHADOW properties: they exist only in the database.
public class InvoiceLineModel
{
    public InvoiceLineModel(Guid productId, decimal amount)
    {
        ProductId = productId;
        Amount = amount;
    }

    public Guid ProductId { get; private set; }

    public decimal Amount { get; private set; }
}
