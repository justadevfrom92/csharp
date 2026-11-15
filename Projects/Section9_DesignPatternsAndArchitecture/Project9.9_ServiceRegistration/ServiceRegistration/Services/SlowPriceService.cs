namespace ServiceRegistration.Services;

// The "real" service: pretends every lookup is slow. Wrapped by CachingPriceServiceDecorator.
public class SlowPriceService : IPriceService
{
    private int lookups;

    public int Lookups => Volatile.Read(ref lookups);

    public decimal GetPrice(int productId)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId), "Product id must be more than 0.");
        }
        Interlocked.Increment(ref lookups);
        return 10m + productId;
    }
}
