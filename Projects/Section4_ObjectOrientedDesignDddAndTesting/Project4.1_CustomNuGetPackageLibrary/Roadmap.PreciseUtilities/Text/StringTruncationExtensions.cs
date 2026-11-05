namespace Roadmap.PreciseUtilities.Text;

/// <summary>Extension methods for shortening display strings without splitting surrogate pairs.</summary>
public static class StringTruncationExtensions
{
    private const char HorizontalEllipsisCharacter = '…';

    /// <summary>
    /// Returns <paramref name="sourceText"/> unchanged when it fits, otherwise cuts it so that the
    /// result INCLUDING the trailing ellipsis is exactly <paramref name="maximumLength"/> characters.
    /// </summary>
    public static string TruncateWithEllipsis(this string sourceText, int maximumLength)
    {
        ArgumentNullException.ThrowIfNull(sourceText);
        Guards.ArgumentRangeGuard.ThrowIfOutsideInclusiveRange(maximumLength, 1, int.MaxValue);

        if (sourceText.Length <= maximumLength)
        {
            return sourceText;
        }

        int keptCharacterCount = maximumLength - 1;
        // Never leave half of an emoji / surrogate pair dangling at the cut point.
        if (keptCharacterCount > 0 && char.IsHighSurrogate(sourceText[keptCharacterCount - 1]))
        {
            keptCharacterCount--;
        }
        return string.Concat(sourceText.AsSpan(0, keptCharacterCount).TrimEnd(), HorizontalEllipsisCharacter.ToString());
    }
}
