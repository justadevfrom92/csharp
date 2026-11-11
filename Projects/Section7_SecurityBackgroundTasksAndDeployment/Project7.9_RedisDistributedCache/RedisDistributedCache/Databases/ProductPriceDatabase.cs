using RedisDistributedCache.Models;

namespace RedisDistributedCache.Databases;

// Pretend database: every read takes 200 ms and is counted, so we can SEE when the cache saves a trip.
public class ProductPriceDatabase
{
    private readonly Dictionary<int, ProductPriceModel> prices = new()
    {
        [1] = new ProductPriceModel(1, "Keyboard", 25.00m),
        [2] = new ProductPriceModel(2, "Mouse", 12.50m),
        [3] = new ProductPriceModel(3, "Monitor", 180.00m),
    };
    private readonly object pricesLock = new();
    private int readCount;

    public int ReadCount
    {
        get
        {
            return Volatile.Read(ref readCount);
        }
    }

    public async Task<ProductPriceModel?> FindAsync(int productId)
    {
        Interlocked.Increment(ref readCount);
        await Task.Delay(200);
        lock (pricesLock)
        {
            return prices.GetValueOrDefault(productId);
        }
    }

    public bool UpdatePrice(int productId, decimal newPrice)
    {
        lock (pricesLock)
        {
            if (!prices.TryGetValue(productId, out ProductPriceModel? product))
            {
                return false;
            }
            prices[productId] = product with { Price = newPrice };
            return true;
        }
    }
}
