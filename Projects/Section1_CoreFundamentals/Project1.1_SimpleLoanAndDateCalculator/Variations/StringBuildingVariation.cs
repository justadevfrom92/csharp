using System.Globalization;
using System.Text;

namespace SimpleLoanAndDateCalculator.Variations;

// "Ann owes 1,234.50" built seven ways. ALL give the same text; they differ in readability and speed.
public static class StringBuildingVariation
{
    private static readonly CultureInfo us = CultureInfo.GetCultureInfo("en-US");

    // + : fine for 2-3 pieces. In a LOOP it creates a new string every time (slow - see 5.3).
    public static string WithPlus(string name, decimal amount)
    {
        return name + " owes " + amount.ToString("N2", us);
    }

    // ORIGINAL: interpolation $"...". The most readable. Uses the CURRENT culture unless you say otherwise...
    public static string WithInterpolation(string name, decimal amount)
    {
        return $"{name} owes {amount.ToString("N2", us)}";
    }

    // ...string.Create with a culture: interpolation that formats EVERY hole with the given culture.
    public static string WithInterpolationAndCulture(string name, decimal amount)
    {
        return string.Create(us, $"{name} owes {amount:N2}");
    }

    // string.Format: the older style, numbered holes. Same result; easy to mix up {0} and {1}.
    public static string WithFormat(string name, decimal amount)
    {
        return string.Format(us, "{0} owes {1:N2}", name, amount);
    }

    // string.Concat: joins pieces with nothing in between.
    public static string WithConcat(string name, decimal amount)
    {
        return string.Concat(name, " owes ", amount.ToString("N2", us));
    }

    // ORIGINAL (the receipt): StringBuilder. Best when adding MANY pieces, especially in a loop.
    public static string WithStringBuilder(string name, decimal amount)
    {
        var builder = new StringBuilder();
        builder.Append(name).Append(" owes ").Append(amount.ToString("N2", us));
        return builder.ToString();
    }

    // string.Join: pieces WITH a separator between them.
    public static string WithJoin(string name, decimal amount)
    {
        return string.Join(" ", name, "owes", amount.ToString("N2", us));
    }
}
