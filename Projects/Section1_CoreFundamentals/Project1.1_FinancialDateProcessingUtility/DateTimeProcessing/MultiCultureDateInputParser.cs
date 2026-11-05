using System.Globalization;

namespace FinancialDateProcessingUtility.DateTimeProcessing;

public sealed record CultureSpecificDateParseResult(DateOnly ParsedDate, CultureInfo MatchedCulture, string MatchedFormatDescription);

/// <summary>
/// Parses user-typed dates. An unambiguous ISO-8601 date (yyyy-MM-dd) is always attempted first;
/// then the caller's preferred culture; then a fixed list of fallback cultures.
/// "03/04/2026" is 4 March in en-US but 3 April in en-GB, so the order of attempts IS the policy.
/// </summary>
public sealed class MultiCultureDateInputParser
{
    private static readonly string[] IsoDateFormats = ["yyyy-MM-dd", "yyyyMMdd"];

    private readonly IReadOnlyList<CultureInfo> _orderedFallbackCultures;

    public MultiCultureDateInputParser(IEnumerable<string> orderedFallbackCultureNames)
    {
        _orderedFallbackCultures = orderedFallbackCultureNames.Select(CultureInfo.GetCultureInfo).ToList();
    }

    public CultureSpecificDateParseResult? TryParseDate(string? rawDateText, CultureInfo? preferredCulture = null)
    {
        string trimmedText = rawDateText?.Trim() ?? string.Empty;
        if (trimmedText.Length == 0)
        {
            return null;
        }

        if (DateOnly.TryParseExact(trimmedText, IsoDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly isoDate))
        {
            return new CultureSpecificDateParseResult(isoDate, CultureInfo.InvariantCulture, "ISO-8601");
        }

        IEnumerable<CultureInfo> candidateCultures = preferredCulture is null
            ? _orderedFallbackCultures
            : _orderedFallbackCultures.Prepend(preferredCulture);

        foreach (CultureInfo candidateCulture in candidateCultures)
        {
            if (DateOnly.TryParse(trimmedText, candidateCulture, DateTimeStyles.AllowWhiteSpaces, out DateOnly cultureDate))
            {
                return new CultureSpecificDateParseResult(cultureDate, candidateCulture, $"culture '{candidateCulture.Name}'");
            }
        }

        return null;
    }
}
