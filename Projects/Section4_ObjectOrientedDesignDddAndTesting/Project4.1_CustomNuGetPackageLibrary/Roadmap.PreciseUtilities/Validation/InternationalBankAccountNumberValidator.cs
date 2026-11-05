using System.Text;

namespace Roadmap.PreciseUtilities.Validation;

/// <summary>Structural and checksum validation of an IBAN (ISO 13616, mod-97 check).</summary>
public static class InternationalBankAccountNumberValidator
{
    /// <summary>
    /// True when the IBAN has a two-letter country code, two check digits, an alphanumeric body,
    /// a length of 15–34 characters (spaces ignored) and passes the ISO 7064 mod-97-10 checksum.
    /// </summary>
    public static bool IsValidInternationalBankAccountNumber(string? ibanText)
    {
        if (string.IsNullOrWhiteSpace(ibanText))
        {
            return false;
        }

        string compactIban = ibanText.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        if (compactIban.Length is < 15 or > 34
            || !char.IsAsciiLetterUpper(compactIban[0]) || !char.IsAsciiLetterUpper(compactIban[1])
            || !char.IsAsciiDigit(compactIban[2]) || !char.IsAsciiDigit(compactIban[3])
            || !compactIban.All(char.IsAsciiLetterOrDigit))
        {
            return false;
        }

        // Move the first four characters to the end, then replace letters with numbers (A=10 … Z=35).
        string rearrangedIban = string.Concat(compactIban.AsSpan(4), compactIban.AsSpan(0, 4));
        int runningRemainder = 0;
        foreach (char character in rearrangedIban)
        {
            int characterValue = char.IsAsciiDigit(character) ? character - '0' : character - 'A' + 10;
            // Process piecewise so the number never exceeds int range (the full value has ~30+ digits).
            runningRemainder = characterValue >= 10
                ? (runningRemainder * 100 + characterValue) % 97
                : (runningRemainder * 10 + characterValue) % 97;
        }
        return runningRemainder == 1;
    }
}
