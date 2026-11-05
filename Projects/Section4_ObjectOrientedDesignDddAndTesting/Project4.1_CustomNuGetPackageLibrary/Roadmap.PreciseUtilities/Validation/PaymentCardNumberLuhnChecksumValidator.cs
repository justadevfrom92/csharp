using Roadmap.PreciseUtilities.Internal;

namespace Roadmap.PreciseUtilities.Validation;

/// <summary>Validates the Luhn (mod 10) checksum used by payment card numbers and IMEIs.</summary>
public static class PaymentCardNumberLuhnChecksumValidator
{
    /// <summary>
    /// True when the digits (spaces and hyphens ignored) pass the Luhn checksum and the length is
    /// between 12 and 19 digits. This detects typos; it does NOT prove the card exists.
    /// </summary>
    public static bool IsValidLuhnChecksum(string? cardNumberText)
    {
        if (!DigitSequenceExtractor.TryExtractDigits(cardNumberText, allowedSeparators: " -", out ReadOnlySpan<char> digits)
            || digits.Length is < 12 or > 19)
        {
            return false;
        }

        int checksumTotal = 0;
        bool shouldDoubleDigit = false;
        for (int digitIndex = digits.Length - 1; digitIndex >= 0; digitIndex--)
        {
            int digitValue = digits[digitIndex] - '0';
            if (shouldDoubleDigit)
            {
                digitValue *= 2;
                if (digitValue > 9)
                {
                    digitValue -= 9;
                }
            }
            checksumTotal += digitValue;
            shouldDoubleDigit = !shouldDoubleDigit;
        }
        return checksumTotal % 10 == 0;
    }
}
