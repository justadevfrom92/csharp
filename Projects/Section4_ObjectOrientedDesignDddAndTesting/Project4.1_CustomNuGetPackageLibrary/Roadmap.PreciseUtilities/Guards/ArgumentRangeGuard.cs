using System.Runtime.CompilerServices;

namespace Roadmap.PreciseUtilities.Guards;

/// <summary>Guard clauses that complement the BCL's ArgumentOutOfRangeException.ThrowIf* helpers.</summary>
public static class ArgumentRangeGuard
{
    /// <summary>
    /// Throws <see cref="ArgumentOutOfRangeException"/> when <paramref name="argumentValue"/> is outside
    /// [<paramref name="inclusiveMinimum"/>, <paramref name="inclusiveMaximum"/>]. The parameter name is
    /// captured automatically with <see cref="CallerArgumentExpressionAttribute"/>.
    /// </summary>
    public static TComparable ThrowIfOutsideInclusiveRange<TComparable>(
        TComparable argumentValue,
        TComparable inclusiveMinimum,
        TComparable inclusiveMaximum,
        [CallerArgumentExpression(nameof(argumentValue))] string? argumentExpression = null)
        where TComparable : IComparable<TComparable>
    {
        if (argumentValue.CompareTo(inclusiveMinimum) < 0 || argumentValue.CompareTo(inclusiveMaximum) > 0)
        {
            throw new ArgumentOutOfRangeException(argumentExpression, argumentValue,
                $"Value must be between {inclusiveMinimum} and {inclusiveMaximum} (inclusive).");
        }
        return argumentValue;
    }
}
