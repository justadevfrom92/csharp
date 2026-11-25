using EfCoreModelling.Models;

namespace EfCoreModelling.Variations;

// The aggregate's list exposed the "obvious" way: IEnumerable over the private List. It LOOKS read-only, but
// the caller can cast it back to List<T> and add lines that skip AddLine's rules (amount > 0).
// InvoiceModel (original) returns linesCollection.AsReadOnly() instead.
public class LeakyInvoiceVariation
{
    private readonly List<InvoiceLineModel> linesCollection = [];

    public IEnumerable<InvoiceLineModel> Lines
    {
        get
        {
            return linesCollection;
        }
    }

    public void AddLine(ProductModel product, decimal amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        linesCollection.Add(new InvoiceLineModel(product.PublicId, amount));
    }
}
