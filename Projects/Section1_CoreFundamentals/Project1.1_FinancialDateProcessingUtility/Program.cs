using System.Globalization;
using System.Text;
using FinancialDateProcessingUtility;
using FinancialDateProcessingUtility.Calculation;
using FinancialDateProcessingUtility.DateTimeProcessing;
using FinancialDateProcessingUtility.Diagnostics;
using FinancialDateProcessingUtility.Formatting;
using FinancialDateProcessingUtility.Validation;

Console.OutputEncoding = Encoding.UTF8;

const decimal MinimumLoanPrincipal = 100m;
const decimal MaximumLoanPrincipal = 10_000_000m;
const decimal MinimumAnnualRatePercent = 0m;
const decimal MaximumAnnualRatePercent = 99.99m;
const int MinimumTermInMonths = 1;
const int MaximumTermInMonths = 480;
const int MaximumInvalidAttemptsPerPrompt = 3;

bool isDemoMode = args.Contains("--demo", StringComparer.OrdinalIgnoreCase);

IConsoleLineInputSource lineInputSource = isDemoMode
    ? new ScriptedConsoleLineInputSource(
    [
        "4",                                       // menu: numeric limits report
        "1",                                       // menu: loan calculator
        "abc", "50", "250000",                     // principal: invalid text, below minimum, valid
        "6.5",                                     // annual rate
        "999", "360",                              // term: above maximum, valid
        "05/03/2027",                              // first payment date (parsed with preferred culture)
        "en-GB",                                   // receipt culture
        "",                                        // customer name left blank -> null-coalesced
        "2",                                       // menu: multi-culture date parsing
        "2027-01-15",
        "3",                                       // menu: time zone conversion
        "2027-03-14T16:30:00+00:00",
        "0"                                        // menu: exit
    ])
    : new InteractiveConsoleLineInputSource();

var multiCultureDateParser = new MultiCultureDateInputParser(["en-US", "en-GB", "de-DE", "fr-FR", "ja-JP"]);
string[] reportedTimeZoneIdentifiers = ["UTC", "America/New_York", "Europe/London", "Europe/Berlin", "Asia/Tokyo", "Australia/Sydney"];

// Null-assignment operator: only fall back to the current culture if no override was configured.
CultureInfo? configuredPreferredCulture = null;
string? preferredCultureEnvironmentValue = Environment.GetEnvironmentVariable("PREFERRED_DATE_CULTURE");
if (!string.IsNullOrWhiteSpace(preferredCultureEnvironmentValue))
{
    configuredPreferredCulture = CultureInfo.GetCultureInfo(preferredCultureEnvironmentValue);
}
configuredPreferredCulture ??= CultureInfo.GetCultureInfo("en-US");

Console.WriteLine("Financial & Date Processing Utility");
Console.WriteLine($"Preferred date culture: {configuredPreferredCulture.Name}{(isDemoMode ? "  [demo mode]" : string.Empty)}");

bool shouldContinueRunning = true;
do
{
    Console.WriteLine();
    Console.WriteLine("1) Loan amortization & receipt   2) Parse a date in multiple cultures");
    Console.WriteLine("3) Convert a time across zones   4) Numeric limits & overflow report   0) Exit");
    string? menuSelection = lineInputSource.ReadNextLine("Choose an option: ");

    // A null selection means input ended (EOF); treat it as exit instead of looping forever.
    shouldContinueRunning = (menuSelection?.Trim()) switch
    {
        null or "0" => false,
        "1" => RunLoanAmortizationWorkflow(),
        "2" => RunMultiCultureDateParsingWorkflow(),
        "3" => RunTimeZoneConversionWorkflow(),
        "4" => PrintNumericLimitsReport(),
        _ => PrintUnknownMenuSelection(menuSelection)
    };
}
while (shouldContinueRunning);

Console.WriteLine("Goodbye.");
return 0;

bool RunLoanAmortizationWorkflow()
{
    decimal? principalAmount = PromptForValidatedValue(
        "Loan principal (100 - 10,000,000): ",
        rawText => BoundedNumericInputValidator.ValidateDecimalWithinInclusiveRange(rawText, MinimumLoanPrincipal, MaximumLoanPrincipal));
    decimal? annualRatePercent = principalAmount is null ? null : PromptForValidatedValue(
        "Annual interest rate % (0 - 99.99): ",
        rawText => BoundedNumericInputValidator.ValidateDecimalWithinInclusiveRange(rawText, MinimumAnnualRatePercent, MaximumAnnualRatePercent));
    int? termInMonths = annualRatePercent is null ? null : PromptForValidatedValue(
        "Term in months (1 - 480): ",
        rawText => BoundedNumericInputValidator.ValidateIntegerWithinInclusiveRange(rawText, MinimumTermInMonths, MaximumTermInMonths));

    if (principalAmount is null || annualRatePercent is null || termInMonths is null)
    {
        Console.WriteLine("Loan calculation cancelled.");
        return true;
    }

    string? rawFirstPaymentDate = lineInputSource.ReadNextLine("First payment date (blank = first day of next month): ");
    DateOnly today = DateOnly.FromDateTime(DateTime.Today);
    DateOnly firstPaymentDate = multiCultureDateParser.TryParseDate(rawFirstPaymentDate, configuredPreferredCulture)?.ParsedDate
        ?? new DateOnly(today.Year, today.Month, 1).AddMonths(1);

    string? rawReceiptCulture = lineInputSource.ReadNextLine("Receipt culture (e.g. en-US, en-GB, de-DE; blank = en-US): ");
    CultureInfo receiptCulture = TryGetCulture(rawReceiptCulture) ?? CultureInfo.GetCultureInfo("en-US");

    string? rawCustomerName = lineInputSource.ReadNextLine("Customer name (optional): ");
    string? customerName = string.IsNullOrWhiteSpace(rawCustomerName) ? null : rawCustomerName.Trim();

    var loanTerms = new FixedRateLoanTerms(principalAmount.Value, annualRatePercent.Value, termInMonths.Value, firstPaymentDate);
    LoanAmortizationSummary amortizationSummary = LoanAmortizationScheduleCalculator.CalculateAmortizationSchedule(loanTerms);
    Console.WriteLine(PaymentReceiptTextFormatter.FormatAmortizationReceipt(loanTerms, amortizationSummary, receiptCulture, customerName));
    return true;
}

bool RunMultiCultureDateParsingWorkflow()
{
    string? rawDateText = lineInputSource.ReadNextLine("Enter a date (any of en-US, en-GB, de-DE, fr-FR, ja-JP, or ISO): ");
    CultureSpecificDateParseResult? parseResult = multiCultureDateParser.TryParseDate(rawDateText, configuredPreferredCulture);

    if (parseResult is null)
    {
        Console.WriteLine($"Could not interpret '{rawDateText ?? string.Empty}' as a date.");
        return true;
    }

    Console.WriteLine($"Parsed using {parseResult.MatchedFormatDescription}: {parseResult.ParsedDate:yyyy-MM-dd} ({parseResult.ParsedDate.DayOfWeek})");
    foreach (string displayCultureName in new[] { "en-US", "en-GB", "de-DE", "ja-JP" })
    {
        CultureInfo displayCulture = CultureInfo.GetCultureInfo(displayCultureName);
        Console.WriteLine($"  {displayCultureName,-6} short: {parseResult.ParsedDate.ToString("d", displayCulture),-12} long: {parseResult.ParsedDate.ToString("D", displayCulture)}");
    }

    DateOnly today = DateOnly.FromDateTime(DateTime.Today);
    int daysFromToday = parseResult.ParsedDate.DayNumber - today.DayNumber;
    string relativeDescription = daysFromToday switch
    {
        0 => "today",
        < 0 and >= -7 => "within the past week",
        < -7 => $"{-daysFromToday} days ago",
        > 0 and <= 7 => "within the next week",
        _ => $"in {daysFromToday} days"
    };
    Console.WriteLine($"  Relative to today: {relativeDescription}");
    return true;
}

bool RunTimeZoneConversionWorkflow()
{
    string? rawInstantText = lineInputSource.ReadNextLine("Enter an instant (e.g. 2027-03-14T16:30:00+00:00; blank = now): ");
    DateTimeOffset instant = DateTimeOffset.TryParse(rawInstantText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset parsedInstant)
        ? parsedInstant
        : DateTimeOffset.UtcNow;

    Console.WriteLine($"Instant (UTC): {instant.UtcDateTime:yyyy-MM-dd HH:mm:ss}Z");
    foreach (TimeZoneConversionResult conversionResult in TimeZoneMeetingTimeConverter.ConvertInstantToTimeZones(instant, reportedTimeZoneIdentifiers))
    {
        Console.WriteLine($"  {conversionResult.TimeZoneIdentifier,-20} {conversionResult.LocalDate:yyyy-MM-dd} {conversionResult.LocalTime:HH:mm}  UTC{(conversionResult.OffsetFromUtc < TimeSpan.Zero ? "-" : "+")}{conversionResult.OffsetFromUtc:hh\\:mm}{(conversionResult.IsDaylightSavingTime ? "  (DST)" : string.Empty)}");
    }

    (int calendarDays, TimeSpan exactDuration) = TimeZoneMeetingTimeConverter.MeasureInterval(DateTimeOffset.UtcNow, instant);
    Console.WriteLine($"  From now: {calendarDays} calendar days ({exactDuration.TotalHours:N1} hours)");
    return true;
}

bool PrintNumericLimitsReport()
{
    Console.WriteLine(NumericTypeLimitsReporter.BuildNumericLimitsReport());
    return true;
}

bool PrintUnknownMenuSelection(string? selection)
{
    Console.WriteLine($"'{selection}' is not a menu option.");
    return true;
}

TNumber? PromptForValidatedValue<TNumber>(string promptText, Func<string?, NumericInputValidationResult<TNumber>> validateRawInput)
    where TNumber : struct
{
    for (int attemptNumber = 1; attemptNumber <= MaximumInvalidAttemptsPerPrompt; attemptNumber++)
    {
        string? rawInput = lineInputSource.ReadNextLine(promptText);
        if (rawInput is null)
        {
            return null; // End of input: abandon the prompt gracefully.
        }

        NumericInputValidationResult<TNumber> validationResult = validateRawInput(rawInput);
        if (validationResult.IsValid)
        {
            return validationResult.ParsedValue;
        }
        Console.WriteLine($"  Rejected: {validationResult.RejectionReason} (attempt {attemptNumber}/{MaximumInvalidAttemptsPerPrompt})");
    }
    return null;
}

static CultureInfo? TryGetCulture(string? cultureName)
{
    if (string.IsNullOrWhiteSpace(cultureName))
    {
        return null;
    }
    try
    {
        return CultureInfo.GetCultureInfo(cultureName.Trim());
    }
    catch (CultureNotFoundException)
    {
        return null;
    }
}
