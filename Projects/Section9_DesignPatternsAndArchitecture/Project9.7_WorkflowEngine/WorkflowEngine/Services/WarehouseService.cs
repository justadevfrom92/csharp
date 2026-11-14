namespace WorkflowEngine.Services;

// Fake warehouse (in memory).
public class WarehouseService
{
    public Dictionary<string, int> Stock { get; } = new() { ["KB-01"] = 10, ["MS-01"] = 1 };

    public List<string> Shipments { get; } = new();

    public void Reserve(string sku, int quantity)
    {
        int available = Stock.GetValueOrDefault(sku);
        if (quantity > available)
        {
            throw new InvalidOperationException($"Only {available} {sku} in stock.");
        }
        Stock[sku] = available - quantity;
    }

    public void Release(string sku, int quantity)
    {
        Stock[sku] = Stock.GetValueOrDefault(sku) + quantity;
    }
}
