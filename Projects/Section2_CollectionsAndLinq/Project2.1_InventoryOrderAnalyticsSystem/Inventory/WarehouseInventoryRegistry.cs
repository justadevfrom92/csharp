using InventoryOrderAnalyticsSystem.Domain;

namespace InventoryOrderAnalyticsSystem.Inventory;

/// <summary>
/// Owns all stock items. Backed by a Dictionary keyed by SKU so that lookups during order
/// processing are O(1) on average regardless of catalog size.
/// </summary>
public sealed class WarehouseInventoryRegistry
{
    private readonly Dictionary<StockKeepingUnitCode, InventoryStockItem> _stockItemsBySku = new();

    /// <summary>Unique set of every category ever registered; Add() returns false for duplicates.</summary>
    private readonly HashSet<string> _registeredProductCategories = new(StringComparer.OrdinalIgnoreCase);

    public int DistinctStockItemCount => _stockItemsBySku.Count;

    /// <summary>Read-only views: callers can enumerate but cannot mutate the internal collections.</summary>
    public IReadOnlyCollection<string> RegisteredProductCategories => _registeredProductCategories;
    public IEnumerable<InventoryStockItem> AllStockItems => _stockItemsBySku.Values;

    public void RegisterStockItem(InventoryStockItem stockItem, params string[] descriptiveTags)
    {
        ArgumentNullException.ThrowIfNull(stockItem);

        // TryAdd avoids a double lookup (ContainsKey + Add) and makes duplicate SKUs explicit.
        if (!_stockItemsBySku.TryAdd(stockItem.StockKeepingUnit, stockItem))
        {
            throw new InvalidOperationException($"SKU {stockItem.StockKeepingUnit} is already registered.");
        }

        _registeredProductCategories.Add(stockItem.ProductCategoryName);
        foreach (string descriptiveTag in descriptiveTags)
        {
            stockItem.DescriptiveTags.Add(descriptiveTag);
        }
    }

    public bool TryFindStockItem(StockKeepingUnitCode stockKeepingUnit, out InventoryStockItem? stockItem) =>
        _stockItemsBySku.TryGetValue(stockKeepingUnit, out stockItem);

    /// <summary>Adds a tag; returns false (and changes nothing) if the tag is already present.</summary>
    public bool TryAddDescriptiveTag(StockKeepingUnitCode stockKeepingUnit, string descriptiveTag) =>
        _stockItemsBySku.TryGetValue(stockKeepingUnit, out InventoryStockItem? stockItem)
        && stockItem.DescriptiveTags.Add(descriptiveTag);
}
