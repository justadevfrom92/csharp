// "Find the product with this code" - five ways. Product is the class at the bottom of Program.cs.
public static class DictionaryLookupVariation
{
    // ORIGINAL: TryGetValue - ONE lookup, no exception when it's missing.
    public static Product? WithTryGetValue(Dictionary<string, Product> stock, string code)
    {
        return stock.TryGetValue(code, out Product? product) ? product : null;
    }

    // ContainsKey + indexer: same result, but looks the key up TWICE.
    public static Product? WithContainsKey(Dictionary<string, Product> stock, string code)
    {
        if (stock.ContainsKey(code))
        {
            return stock[code];
        }
        return null;
    }

    // GetValueOrDefault: same result in one short call (returns null for a missing class value).
    public static Product? WithGetValueOrDefault(Dictionary<string, Product> stock, string code)
    {
        return stock.GetValueOrDefault(code);
    }

    // Indexer only: DIFFERENT - a missing key THROWS KeyNotFoundException. Fine when missing = a bug.
    public static Product WithIndexer(Dictionary<string, Product> stock, string code)
    {
        return stock[code];
    }

    // A List searched with FirstOrDefault: same answer, but checks items ONE BY ONE (O(n)) instead of jumping (O(1)).
    public static Product? WithListSearch(List<Product> products, string code)
    {
        return products.FirstOrDefault(product => product.Code == code);
    }
}
