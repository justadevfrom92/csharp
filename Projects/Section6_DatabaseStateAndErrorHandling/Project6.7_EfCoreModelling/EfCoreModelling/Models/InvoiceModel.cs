namespace EfCoreModelling.Models;

// An AGGREGATE (DDD, see 4.2): the invoice owns its lines, and lines can only be added THROUGH the invoice,
// so its rules always run. EF fills the PRIVATE list "linesCollection" (configured by name in
// InvoiceConfiguration); outside code only gets a read-only view.
public class InvoiceModel
{
    private readonly List<InvoiceLineModel> linesCollection = [];

    public InvoiceModel(string customer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customer);
        Customer = customer;
    }

    public Guid PublicId { get; private set; } = Guid.NewGuid();

    public string Customer { get; private set; }

    // AsReadOnly: a wrapper that throws on Add. Returning the List itself as IEnumerable could be cast back
    // to List<T> and changed (see Variations/LeakyInvoiceVariation).
    public IReadOnlyList<InvoiceLineModel> Lines
    {
        get
        {
            return linesCollection.AsReadOnly();
        }
    }

    public void AddLine(ProductModel product, decimal amount)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        linesCollection.Add(new InvoiceLineModel(product.PublicId, amount));
    }
}
