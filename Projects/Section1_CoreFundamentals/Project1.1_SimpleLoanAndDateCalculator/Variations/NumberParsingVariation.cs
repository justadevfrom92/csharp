using System.Globalization;

namespace SimpleLoanAndDateCalculator.Variations;

// Turning text into a decimal. Same job, very different behaviour on BAD input and in OTHER COUNTRIES.
public static class NumberParsingVariation
{
    // ORIGINAL (Program.cs): TryParse + InvariantCulture. Bad text -> false, no exception. "1.5" always means one and a half.
    public static decimal? WithTryParseInvariant(string? text)
    {
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value) ? value : null;
    }

    // Parse: same answer for good text, but THROWS FormatException for "abc" and ArgumentNullException for null.
    // Exceptions are slow and noisy for something that happens all the time (users mistype), so TryParse is preferred.
    public static decimal WithParse(string text)
    {
        return decimal.Parse(text, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    // Convert.ToDecimal: null becomes 0 (!) instead of an error; "abc" still throws. Easy to hide a missing value.
    public static decimal WithConvert(string? text)
    {
        return Convert.ToDecimal(text, CultureInfo.InvariantCulture);
    }

    // TryParse with a SPECIFIC culture: in Germany "," is the decimal point and "." groups thousands.
    //   "1,5" -> 1.5 in de-DE, but 15 in InvariantCulture (the comma is read as a thousands separator).
    public static decimal? WithTryParseCulture(string? text, string cultureName)
    {
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.GetCultureInfo(cultureName), out decimal value) ? value : null;
    }

    // Without a culture: uses the COMPUTER'S settings, so the same program gives different answers on different machines.
    public static decimal? WithTryParseCurrentCulture(string? text)
    {
        return decimal.TryParse(text, out decimal value) ? value : null;
    }
}
