namespace Roadmap.PreciseUtilities.Internal;

/// <summary>
/// INTERNAL on purpose: an implementation detail shared by validators. Keeping it out of the
/// public API means it can change in a patch release without breaking any consumer.
/// </summary>
internal static class DigitSequenceExtractor
{
    public static bool TryExtractDigits(string? sourceText, string allowedSeparators, out ReadOnlySpan<char> digits)
    {
        digits = default;
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return false;
        }

        var digitBuffer = new char[sourceText.Length];
        int digitCount = 0;
        foreach (char character in sourceText)
        {
            if (char.IsAsciiDigit(character))
            {
                digitBuffer[digitCount++] = character;
            }
            else if (!allowedSeparators.Contains(character))
            {
                return false;
            }
        }
        digits = digitBuffer.AsSpan(0, digitCount);
        return digitCount > 0;
    }
}
