using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OutputCachingCatalog.Models;

public record CatalogProduct(int Id, string Name, decimal Price);

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

// Custom health check: is the "database" answering fast enough?
public class CatalogDatabaseHealthCheck : IHealthCheck
{
    private readonly CatalogDatabase database;

    public CatalogDatabaseHealthCheck(CatalogDatabase database)
    {
        this.database = database;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        List<CatalogProduct> products = await database.LoadAllProductsAsync();
        if (products.Count == 0)
        {
            return HealthCheckResult.Unhealthy("Catalog is empty.");
        }
        if (timer.ElapsedMilliseconds > 1000)
        {
            return HealthCheckResult.Degraded($"Catalog is slow: {timer.ElapsedMilliseconds} ms");
        }
        return HealthCheckResult.Healthy($"{products.Count} products in {timer.ElapsedMilliseconds} ms");
    }
}
