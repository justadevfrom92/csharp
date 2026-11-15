using System.Text.RegularExpressions;

// Read the milliseconds (3rd word) from "2026-11-05T10:00:00Z ERROR 1234 /api/orders" - four ways.
// Same number every time; very different amounts of memory allocated per line.
public static partial class LogLineParseVariation
{
    // string.Split: creates an array and a new string for EVERY word.
    public static int WithSplit(string line)
    {
        return int.Parse(line.Split(' ')[2]);
    }

    // ORIGINAL idea: Span - slices the existing text, creates nothing.
    public static int WithSpan(string line)
    {
        ReadOnlySpan<char> rest = line.AsSpan();
        rest = rest[(rest.IndexOf(' ') + 1)..];
        rest = rest[(rest.IndexOf(' ') + 1)..];
        int space = rest.IndexOf(' ');
        return int.Parse(space < 0 ? rest : rest[..space]);
    }

    // IndexOf + Substring: no array, but still one new string for the number.
    public static int WithSubstring(string line)
    {
        int first = line.IndexOf(' ');
        int second = line.IndexOf(' ', first + 1);
        int third = line.IndexOf(' ', second + 1);
        return int.Parse(line.Substring(second + 1, (third < 0 ? line.Length : third) - second - 1));
    }

    // Regex (source-generated, so no runtime compile): easiest to change, allocates a Match object per line.
    [GeneratedRegex(@"^\S+ \S+ (\d+)")]
    private static partial Regex MillisecondsPattern();

    public static int WithRegex(string line)
    {
        return int.Parse(MillisecondsPattern().Match(line).Groups[1].ValueSpan);
    }
}
