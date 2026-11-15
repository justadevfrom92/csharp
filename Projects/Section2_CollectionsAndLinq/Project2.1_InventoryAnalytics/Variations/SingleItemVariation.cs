// Getting ONE item: the names look alike, the behaviour on 0 or 2+ matches does not.
//
//                   0 matches                   1 match   2+ matches
//   First           InvalidOperationException   the item  the FIRST one
//   FirstOrDefault  null                        the item  the FIRST one
//   Single          InvalidOperationException   the item  InvalidOperationException
//   SingleOrDefault null                        the item  InvalidOperationException
//   Last / LastOrDefault  like First, from the end
//   ElementAt(i)    ArgumentOutOfRangeException if i is too big
public static class SingleItemVariation
{
    public static Product First(IEnumerable<Product> products, string category)
    {
        return products.First(product => product.Category == category);
    }

    public static Product? FirstOrDefault(IEnumerable<Product> products, string category)
    {
        return products.FirstOrDefault(product => product.Category == category);
    }

    public static Product Single(IEnumerable<Product> products, string category)
    {
        return products.Single(product => product.Category == category);
    }

    public static Product? SingleOrDefault(IEnumerable<Product> products, string category)
    {
        return products.SingleOrDefault(product => product.Category == category);
    }

    // "Is there any?" - Any() stops at the first match; Count() > 0 counts EVERY item first. Same answer.
    public static bool HasAnyWithAny(IEnumerable<Product> products)
    {
        return products.Any(product => product.QuantityInStock == 0);
    }

    public static bool HasAnyWithCount(IEnumerable<Product> products)
    {
        return products.Count(product => product.QuantityInStock == 0) > 0;
    }
}
