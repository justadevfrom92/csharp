using System.Text;

namespace Benchmarks.Builders;

// Joins values into "a,b,c". Three ways, same result, very different cost.
public static class CsvLineBuilder
{
    public static string BuildWithPlusEquals(IReadOnlyList<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        string line = "";
        for (int i = 0; i < values.Count; i++)
        {
            line += (i == 0 ? "" : ",") + values[i]; // a brand-new string every time round the loop
        }
        return line;
    }

    public static string BuildWithStringBuilder(IReadOnlyList<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var builder = new StringBuilder();
        for (int i = 0; i < values.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }
            builder.Append(values[i]); // adds to one growing buffer
        }
        return builder.ToString();
    }

    public static string BuildWithJoin(IReadOnlyList<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return string.Join(',', values); // works out the final length first, copies once
    }
}
