namespace FinancialDateProcessingUtility.DateTimeProcessing;

public sealed record TimeZoneConversionResult(
    string TimeZoneIdentifier,
    DateTimeOffset LocalDateTimeOffset,
    DateOnly LocalDate,
    TimeOnly LocalTime,
    TimeSpan OffsetFromUtc,
    bool IsDaylightSavingTime);

/// <summary>
/// Converts a single instant to several time zones. DateTimeOffset is used (not DateTime)
/// because it carries its UTC offset, so an instant can never be silently re-interpreted as
/// "local" or "unspecified" kind.
/// </summary>
public static class TimeZoneMeetingTimeConverter
{
    public static IReadOnlyList<TimeZoneConversionResult> ConvertInstantToTimeZones(
        DateTimeOffset instant,
        IEnumerable<string> ianaTimeZoneIdentifiers)
    {
        var conversionResults = new List<TimeZoneConversionResult>();

        foreach (string timeZoneIdentifier in ianaTimeZoneIdentifiers)
        {
            TimeZoneInfo? timeZone = FindTimeZoneOrDefault(timeZoneIdentifier);
            if (timeZone is null)
            {
                continue; // Unknown zones are skipped rather than crashing the whole report.
            }

            DateTimeOffset localInstant = TimeZoneInfo.ConvertTime(instant, timeZone);
            conversionResults.Add(new TimeZoneConversionResult(
                timeZone.Id,
                localInstant,
                DateOnly.FromDateTime(localInstant.DateTime),
                TimeOnly.FromDateTime(localInstant.DateTime),
                localInstant.Offset,
                timeZone.IsDaylightSavingTime(localInstant)));
        }

        return conversionResults;
    }

    public static TimeZoneInfo? FindTimeZoneOrDefault(string timeZoneIdentifier)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneIdentifier);
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
    }

    /// <summary>Whole calendar days between two dates, plus the TimeSpan between two instants.</summary>
    public static (int CalendarDaysBetween, TimeSpan ExactDuration) MeasureInterval(DateTimeOffset start, DateTimeOffset end) =>
        (DateOnly.FromDateTime(end.UtcDateTime).DayNumber - DateOnly.FromDateTime(start.UtcDateTime).DayNumber, end - start);
}
