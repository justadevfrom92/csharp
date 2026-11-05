using System.Diagnostics;
using InventoryOrderAnalyticsSystem.Domain;

namespace InventoryOrderAnalyticsSystem.Analytics;

public sealed record LookupBenchmarkResult(int CatalogSize, int LookupCount, TimeSpan LinearListScanDuration, TimeSpan DictionaryHashLookupDuration)
{
    public double SpeedupFactor => LinearListScanDuration.TotalMilliseconds / Math.Max(DictionaryHashLookupDuration.TotalMilliseconds, 0.0001);
}

/// <summary>
/// Measures the trade-off documented in the README: List.Find is O(n) per lookup, a Dictionary
/// lookup is O(1) on average. With n items and m lookups that is O(n·m) vs O(m).
/// </summary>
public static class CollectionLookupPerformanceComparison
{
    public static LookupBenchmarkResult CompareListScanWithDictionaryLookup(int catalogSize, int lookupCount)
    {
        List<InventoryStockItem> catalogAsList = Enumerable.Range(1, catalogSize)
            .Select(itemNumber => new InventoryStockItem
            {
                StockKeepingUnit = new StockKeepingUnitCode($"BENCH-{itemNumber:D6}"),
                ProductDisplayName = $"Benchmark item {itemNumber}",
                ProductCategoryName = "Benchmark",
                UnitPrice = 1m,
                ReorderThresholdQuantity = 0,
                QuantityOnHand = 1
            })
            .ToList();
        Dictionary<StockKeepingUnitCode, InventoryStockItem> catalogAsDictionary =
            catalogAsList.ToDictionary(stockItem => stockItem.StockKeepingUnit);

        var randomNumberGenerator = new Random(Seed: 42);
        StockKeepingUnitCode[] lookupKeys = Enumerable.Range(0, lookupCount)
            .Select(_ => new StockKeepingUnitCode($"BENCH-{randomNumberGenerator.Next(1, catalogSize + 1):D6}"))
            .ToArray();

        int foundByListScan = 0;
        Stopwatch listStopwatch = Stopwatch.StartNew();
        foreach (StockKeepingUnitCode lookupKey in lookupKeys)
        {
            if (catalogAsList.Find(stockItem => stockItem.StockKeepingUnit == lookupKey) is not null)
            {
                foundByListScan++;
            }
        }
        listStopwatch.Stop();

        int foundByDictionary = 0;
        Stopwatch dictionaryStopwatch = Stopwatch.StartNew();
        foreach (StockKeepingUnitCode lookupKey in lookupKeys)
        {
            if (catalogAsDictionary.TryGetValue(lookupKey, out _))
            {
                foundByDictionary++;
            }
        }
        dictionaryStopwatch.Stop();

        if (foundByListScan != foundByDictionary)
        {
            throw new InvalidOperationException("Both strategies must find the same items.");
        }

        return new LookupBenchmarkResult(catalogSize, lookupCount, listStopwatch.Elapsed, dictionaryStopwatch.Elapsed);
    }
}
