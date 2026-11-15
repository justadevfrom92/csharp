using System.Numerics;

namespace SimpleLoanAndDateCalculator.Variations;

// Adding 1 to the biggest int, six ways.
public static class OverflowVariation
{
    // Default (no keyword): wraps silently to a negative number. Same as unchecked.
    public static int Default(int value)
    {
        return value + 1;
    }

    // ORIGINAL: unchecked - written out to make the wrap-around visible. Same result as Default.
    public static int Unchecked(int value)
    {
        return unchecked(value + 1);
    }

    // ORIGINAL: checked - throws OverflowException instead of wrapping.
    public static int Checked(int value)
    {
        return checked(value + 1);
    }

    // A checked BLOCK: every int operation inside is checked, not just one expression.
    public static int CheckedBlock(int value)
    {
        checked
        {
            int doubled = value * 2;
            return doubled + 1;
        }
    }

    // A BIGGER TYPE: long can hold int.MaxValue + 1, so there's no overflow at all (until long's own limit).
    public static long WithLong(int value)
    {
        return (long)value + 1;
    }

    // BigInteger: no limit at all (slower, more memory). For huge numbers like factorials or cryptography.
    public static BigInteger WithBigInteger(int value)
    {
        return new BigInteger(value) + 1;
    }

    // Project-wide alternative (no code here): <CheckForOverflowUnderflow>true</CheckForOverflowUnderflow> in the .csproj
    // makes EVERY int operation checked by default; then "unchecked" is the exception you write out.
}
