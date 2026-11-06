namespace OutputCachingCatalog.Models;

// Pretend database: every read takes 100 ms and is counted, so we can SEE how often it's hit.
public class CatalogDatabase
{
    private readonly List<CatalogProduct> products = new();
    private readonly object listLock = new();
    private int readCount;

    public CatalogDatabase()
    {
        for (int id = 1; id <= 20; id++)
        {
            products.Add(new CatalogProduct(id, $"Product {id}", 10m + id));
        }
    }

    public int ReadCount
    {
        get
        {
            return Volatile.Read(ref readCount);
        }
    }

    public async Task<List<CatalogProduct>> LoadAllProductsAsync()
    {
        Interlocked.Increment(ref readCount);
        await Task.Delay(100); // slow query
        lock (listLock)
        {
            return products.ToList();
        }
    }

    public void ChangePrice(int productId, decimal newPrice)
    {
        lock (listLock)
        {
            int index = products.FindIndex(product => product.Id == productId);
            if (index >= 0)
            {
                products[index] = products[index] with { Price = newPrice };
            }
        }
    }
}
