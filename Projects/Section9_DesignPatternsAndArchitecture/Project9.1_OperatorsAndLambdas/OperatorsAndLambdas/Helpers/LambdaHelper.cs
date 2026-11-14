using System.Linq.Expressions;
using OperatorsAndLambdas.Models;

namespace OperatorsAndLambdas.Helpers;

// LAMBDA EXPRESSIONS: small unnamed functions, written  (parameters) => result.
// They are VALUES: you can store them in variables, pass them to methods and return them from methods.
public static class LambdaHelper
{
    // A lambda RETURNED from a method. "factor" is CAPTURED: the lambda remembers it after this method ends.
    // This is called a CLOSURE.
    public static Func<decimal, decimal> MakeMultiplier(decimal factor)
    {
        if (factor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(factor), "Factor must be more than 0.");
        }
        return price => price * factor;
    }

    // Lambdas TAKEN as parameters and joined into a new one: first runs "first", then "second" on its result.
    public static Func<T, T> Compose<T>(Func<T, T> first, Func<T, T> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        return value => second(first(value));
    }

    // Applies a whole list of price rules, in order. Each rule is just a Func<decimal, decimal>.
    public static decimal ApplyAll(decimal price, IEnumerable<Func<decimal, decimal>> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price can't be negative.");
        }
        decimal result = price;
        foreach (Func<decimal, decimal> rule in rules)
        {
            result = rule(result);
        }
        return result;
    }

    // A closure that keeps STATE between calls: every call adds 1 to the captured "count".
    public static Func<int> MakeCounter()
    {
        int count = 0;
        return () => ++count;
    }

    // Func<T, bool> vs Expression<Func<T, bool>>:
    //   Func       = compiled code. You can only RUN it.
    //   Expression = the lambda as a DATA TREE. You can READ it (this method turns it back into text),
    //                and that's how EF Core turns  p => p.Price > 10  into  WHERE Price > 10.
    public static string Describe(Expression<Func<ProductModel, bool>> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        return condition.Body.ToString();
    }

    // The same Expression can still be COMPILED into a Func and run.
    public static List<ProductModel> Filter(IEnumerable<ProductModel> products, Expression<Func<ProductModel, bool>> condition)
    {
        ArgumentNullException.ThrowIfNull(products);
        ArgumentNullException.ThrowIfNull(condition);
        Func<ProductModel, bool> compiled = condition.Compile();
        return products.Where(compiled).ToList();
    }
}
