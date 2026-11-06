using OutputCachingCatalog.Data;
using OutputCachingCatalog.Data.Entities;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OutputCachingCatalog.HealthChecks;

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
