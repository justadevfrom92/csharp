// "Names of products with 5 or fewer left, fewest first" - the same query four ways.
public static class LinqStyleVariation
{
    // ORIGINAL: method syntax (Where / OrderBy / Select).
    public static List<string> WithMethodSyntax(IEnumerable<Product> products)
    {
        return products.Where(product => product.QuantityInStock <= 5).OrderBy(product => product.QuantityInStock).Select(product => product.Name).ToList();
    }

    // Query syntax: looks like SQL. The compiler turns it into the method calls above - identical result.
    public static List<string> WithQuerySyntax(IEnumerable<Product> products)
    {
        IEnumerable<string> query =
            from product in products
            where product.QuantityInStock <= 5
            orderby product.QuantityInStock
            select product.Name;
        return query.ToList();
    }

    // Plain loop + List.Sort: what LINQ does for you, written out. Sort is "stable" in LINQ's OrderBy
    // (equal items keep their order) but NOT in List.Sort - so ties may come out in a different order.
    public static List<string> WithLoopAndSort(IEnumerable<Product> products)
    {
        var lowStock = new List<Product>();
        foreach (Product product in products)
        {
            if (product.QuantityInStock <= 5)
            {
                lowStock.Add(product);
            }
        }
        lowStock.Sort((left, right) => left.QuantityInStock.CompareTo(right.QuantityInStock));
        var names = new List<string>();
        foreach (Product product in lowStock)
        {
            names.Add(product.Name);
        }
        return names;
    }

    // Group totals: GroupBy (DEFERRED: runs each time you loop) vs ToLookup (IMMEDIATE: grouped once, then reused).
    public static Dictionary<string, decimal> ValueByCategoryWithGroupBy(IEnumerable<Product> products)
    {
        return products.GroupBy(product => product.Category).ToDictionary(group => group.Key, group => group.Sum(product => product.Price * product.QuantityInStock));
    }

    public static Dictionary<string, decimal> ValueByCategoryWithToLookup(IEnumerable<Product> products)
    {
        ILookup<string, Product> byCategory = products.ToLookup(product => product.Category);
        return byCategory.ToDictionary(group => group.Key, group => group.Sum(product => product.Price * product.QuantityInStock));
    }

    // Same total, three ways: Sum, Aggregate (general "fold"), and a loop.
    public static decimal TotalWithSum(IEnumerable<Product> products)
    {
        return products.Sum(product => product.Price * product.QuantityInStock);
    }

    public static decimal TotalWithAggregate(IEnumerable<Product> products)
    {
        return products.Aggregate(0m, (total, product) => total + product.Price * product.QuantityInStock);
    }
}
