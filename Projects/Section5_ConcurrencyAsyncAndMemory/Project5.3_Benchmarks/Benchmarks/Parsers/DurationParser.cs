using System.Globalization;

namespace Benchmarks.Parsers;

// Reads the milliseconds out of a log line: "<timestamp> <LEVEL> <milliseconds> <path>".
// Two versions with the SAME rules, so the benchmark compares only the technique.
public static class DurationParser
{
    public static int ParseWithSplit(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        string[] parts = line.Split(' '); // new array + a new string for every word
        if (parts.Length < 4)
        {
            throw new FormatException("Expected '<timestamp> <LEVEL> <milliseconds> <path>'.");
        }
        return CheckDuration(int.Parse(parts[2], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture));
    }

    public static int ParseWithSpan(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        ReadOnlySpan<char> rest = line.AsSpan(); // a "window" onto the string: nothing is copied
        for (int wordToSkip = 0; wordToSkip < 2; wordToSkip++)
        {
            int space = rest.IndexOf(' ');
            if (space < 0)
            {
                throw new FormatException("Expected '<timestamp> <LEVEL> <milliseconds> <path>'.");
            }
            rest = rest.Slice(space + 1);
        }
        int end = rest.IndexOf(' ');
        if (end < 0)
        {
            throw new FormatException("Expected '<timestamp> <LEVEL> <milliseconds> <path>'.");
        }
        return CheckDuration(int.Parse(rest.Slice(0, end), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture));
    }

    private static int CheckDuration(int milliseconds)
    {
        if (milliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(milliseconds), "A duration can't be negative.");
        }
        return milliseconds;
    }
}
