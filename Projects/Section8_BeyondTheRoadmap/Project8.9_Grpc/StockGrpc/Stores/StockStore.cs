namespace StockGrpc.Stores;

// In-memory stock levels, shared by all calls (singleton). Raises StockChanged so streams can push updates.
public class StockStore
{
    private readonly Dictionary<string, (string Name, int Quantity)> stock = new()
    {
        ["KB-01"] = ("Keyboard", 10),
        ["MS-01"] = ("Mouse", 3),
    };
    private readonly object stockLock = new();

    public event Action<string>? StockChanged;

    public (string Name, int Quantity)? Find(string sku)
    {
        lock (stockLock)
        {
            return stock.TryGetValue(sku, out var item) ? item : null;
        }
    }

    // Returns the new quantity, or null when there isn't enough stock.
    public int? TryReserve(string sku, int quantity)
    {
        int remaining;
        lock (stockLock)
        {
            if (!stock.TryGetValue(sku, out var item) || item.Quantity < quantity)
            {
                return null;
            }
            remaining = item.Quantity - quantity;
            stock[sku] = (item.Name, remaining);
        }
        StockChanged?.Invoke(sku); // outside the lock, so listeners can read the store
        return remaining;
    }
}
