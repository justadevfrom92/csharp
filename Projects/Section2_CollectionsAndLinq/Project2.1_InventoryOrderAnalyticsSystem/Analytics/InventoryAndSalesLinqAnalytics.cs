using InventoryOrderAnalyticsSystem.Domain;

namespace InventoryOrderAnalyticsSystem.Analytics;

public sealed record CategoryInventoryValuation(string CategoryName, int DistinctProductCount, int TotalUnitsOnHand, decimal TotalStockValue);

public sealed record LowStockAlert(StockKeepingUnitCode StockKeepingUnit, string ProductDisplayName, int QuantityOnHand, int ReorderThresholdQuantity, int SuggestedReorderQuantity);

public sealed record CategorySalesReport(
    string CategoryName,
    int OrderCount,
    int TotalUnitsSold,
    decimal TotalRevenue,
    decimal AverageLineRevenue,
    decimal LargestSingleLineRevenue,
    StockKeepingUnitCode BestSellingStockKeepingUnit);

/// <summary>
/// All analytics take IEnumerable&lt;T&gt; so they work with any source (List, Dictionary.Values,
/// arrays, or a lazy generator). Each method materialises its result with ToList() so the caller
/// receives a stable snapshot rather than a query that re-runs against mutated inventory later.
/// </summary>
public static class InventoryAndSalesLinqAnalytics
{
    public static decimal CalculateTotalInventoryValue(IEnumerable<InventoryStockItem> stockItems) =>
        stockItems.Sum(stockItem => stockItem.TotalStockValue);

    public static IReadOnlyList<CategoryInventoryValuation> CalculateInventoryValueByCategory(IEnumerable<InventoryStockItem> stockItems) =>
        stockItems
            .GroupBy(stockItem => stockItem.ProductCategoryName, StringComparer.OrdinalIgnoreCase)
            .Select(categoryGroup => new CategoryInventoryValuation(
                categoryGroup.Key,
                categoryGroup.Count(),
                categoryGroup.Sum(stockItem => stockItem.QuantityOnHand),
                categoryGroup.Sum(stockItem => stockItem.TotalStockValue)))
            .OrderByDescending(valuation => valuation.TotalStockValue)
            .ThenBy(valuation => valuation.CategoryName)
            .ToList();

    public static IReadOnlyList<LowStockAlert> FindLowStockItems(IEnumerable<InventoryStockItem> stockItems) =>
        stockItems
            .Where(stockItem => stockItem.IsAtOrBelowReorderThreshold)
            .OrderBy(stockItem => stockItem.QuantityOnHand)
            .Select(stockItem => new LowStockAlert(
                stockItem.StockKeepingUnit,
                stockItem.ProductDisplayName,
                stockItem.QuantityOnHand,
                stockItem.ReorderThresholdQuantity,
                // Restock to double the threshold: a simple, explainable reorder policy.
                stockItem.ReorderThresholdQuantity * 2 - stockItem.QuantityOnHand))
            .ToList();

    public static IReadOnlyList<CategorySalesReport> BuildSalesReportByCategory(IEnumerable<FulfilledSaleRecord> fulfilledSales) =>
        fulfilledSales
            .GroupBy(sale => sale.ProductCategoryName)
            .Select(categorySales => new CategorySalesReport(
                categorySales.Key,
                categorySales.Select(sale => sale.OrderNumber).Distinct().Count(),
                categorySales.Sum(sale => sale.QuantitySold),
                categorySales.Sum(sale => sale.LineRevenue),
                Math.Round(categorySales.Average(sale => sale.LineRevenue), 2),
                categorySales.Max(sale => sale.LineRevenue),
                categorySales
                    .GroupBy(sale => sale.StockKeepingUnit)
                    .MaxBy(skuGroup => skuGroup.Sum(sale => sale.QuantitySold))!.Key))
            .OrderByDescending(report => report.TotalRevenue)
            .ToList();

    public static IReadOnlyList<(string TagName, int ProductCount)> CountProductsPerTag(IEnumerable<InventoryStockItem> stockItems) =>
        stockItems
            .SelectMany(stockItem => stockItem.DescriptiveTags)
            .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .Select(tagGroup => (tagGroup.Key, tagGroup.Count()))
            .OrderByDescending(tagCount => tagCount.Item2)
            .ThenBy(tagCount => tagCount.Key)
            .ToList();
}
