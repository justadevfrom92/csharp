using System.Globalization;

namespace SimpleLoanAndDateCalculator.Variations;

// "What day is the payment?" - which date type and which parse method.
public static class DateVariation
{
    // ORIGINAL: DateOnly - a calendar day, no time, no time zone. Can't be confused with "midnight in some zone".
    public static DateOnly AsDateOnly(int year, int month, int day)
    {
        return new DateOnly(year, month, day);
    }

    // DateTime: carries a time (00:00) and a Kind (Unspecified here). Comparing it with a UTC DateTime can be off by hours.
    public static DateTime AsDateTime(int year, int month, int day)
    {
        return new DateTime(year, month, day);
    }

    // DateTimeOffset: an exact MOMENT with its UTC offset. Right for "when did it happen", wrong for "which day".
    public static DateTimeOffset AsDateTimeOffset(int year, int month, int day)
    {
        return new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero);
    }

    // ORIGINAL first try: ParseExact with ONE format - "01/02/2027" is rejected, "2027-02-01" accepted. No guessing.
    public static DateOnly? WithTryParseExact(string text)
    {
        return DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date) ? date : null;
    }

    // TryParse with a culture: "01/02/2027" is 2 January in en-US but 1 February in en-GB. SAME text, DIFFERENT day.
    public static DateOnly? WithTryParseCulture(string text, string cultureName)
    {
        return DateOnly.TryParse(text, CultureInfo.GetCultureInfo(cultureName), DateTimeStyles.None, out DateOnly date) ? date : null;
    }
}
