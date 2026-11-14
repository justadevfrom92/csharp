namespace PipelinePatterns.Services;

// Plain business code. It has NO logging or timing code in it - the interceptor adds that from outside.
public class StockService : IStockService
{
    private readonly Dictionary<string, int> stock = new() { ["KB-01"] = 10, ["MS-01"] = 2 };

    public int GetStock(string sku)
    {
        return stock.TryGetValue(sku, out int quantity) ? quantity : throw new KeyNotFoundException($"No product {sku}.");
    }

    public void Reserve(string sku, int quantity)
    {
        int available = GetStock(sku);
        if (quantity > available)
        {
            throw new InvalidOperationException($"Only {available} {sku} left.");
        }
        stock[sku] = available - quantity;
    }
}
