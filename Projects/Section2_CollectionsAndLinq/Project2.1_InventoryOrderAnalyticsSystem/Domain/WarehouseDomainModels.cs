namespace InventoryOrderAnalyticsSystem.Domain;

/// <summary>
/// Stock-keeping unit code. A dedicated record struct (instead of a bare string) prevents mixing
/// up an SKU with a product name, and gives value equality + a good hash code for Dictionary keys.
/// </summary>
public readonly record struct StockKeepingUnitCode
{
    public string Value { get; }

    public StockKeepingUnitCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        // Normalised once here so "sku-1" and "SKU-1" are the same dictionary key.
        Value = value.Trim().ToUpperInvariant();
    }

    public override string ToString() => Value;
}

public sealed class InventoryStockItem
{
    public required StockKeepingUnitCode StockKeepingUnit { get; init; }
    public required string ProductDisplayName { get; init; }
    public required string ProductCategoryName { get; init; }
    public required decimal UnitPrice { get; init; }
    public required int ReorderThresholdQuantity { get; init; }
    public int QuantityOnHand { get; set; }

    /// <summary>
    /// Case-insensitive tag set: HashSet rejects duplicates in O(1) and ignores "Fragile" vs "fragile".
    /// </summary>
    public HashSet<string> DescriptiveTags { get; } = new(StringComparer.OrdinalIgnoreCase);

    public decimal TotalStockValue => UnitPrice * QuantityOnHand;
    public bool IsAtOrBelowReorderThreshold => QuantityOnHand <= ReorderThresholdQuantity;
}

public sealed record CustomerOrderLine(StockKeepingUnitCode StockKeepingUnit, int RequestedQuantity);

public sealed record CustomerOrder(int OrderNumber, string CustomerName, DateTimeOffset PlacedAt, IReadOnlyList<CustomerOrderLine> OrderLines);

public enum OrderFulfillmentStatus
{
    Fulfilled,
    RejectedInsufficientStock,
    RejectedUnknownProduct
}

public sealed record FulfilledSaleRecord(
    int OrderNumber,
    StockKeepingUnitCode StockKeepingUnit,
    string ProductCategoryName,
    int QuantitySold,
    decimal UnitPriceAtSale,
    DateTimeOffset SoldAt)
{
    public decimal LineRevenue => QuantitySold * UnitPriceAtSale;
}

public sealed record OrderProcessingOutcome(CustomerOrder Order, OrderFulfillmentStatus Status, string Explanation);
