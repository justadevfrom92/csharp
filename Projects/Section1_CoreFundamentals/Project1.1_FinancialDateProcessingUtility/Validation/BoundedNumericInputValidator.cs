using System.Globalization;

namespace FinancialDateProcessingUtility.Validation;

/// <summary>
/// The outcome of validating a single raw numeric input. Using a result object (instead of
/// throwing) lets the interactive loop re-prompt the user without exception-driven control flow.
/// </summary>
public readonly record struct NumericInputValidationResult<TNumber>(
    bool IsValid,
    TNumber? ParsedValue,
    string? RejectionReason)
    where TNumber : struct
{
    public static NumericInputValidationResult<TNumber> Accepted(TNumber value) => new(true, value, null);
    public static NumericInputValidationResult<TNumber> Rejected(string reason) => new(false, null, reason);
}

/// <summary>
/// Parses raw text into numbers and rejects values outside an explicit inclusive range.
/// The range is always checked against the type's own MinValue/MaxValue first, so a caller can
/// never configure a bound the underlying type cannot represent.
/// </summary>
public static class BoundedNumericInputValidator
{
    public static NumericInputValidationResult<decimal> ValidateDecimalWithinInclusiveRange(
        string? rawInput,
        decimal minimumAllowedValue,
        decimal maximumAllowedValue,
        IFormatProvider? formatProvider = null)
    {
        // Null-coalescing: a null line (Ctrl+D / redirected EOF) is treated exactly like empty input.
        string trimmedInput = rawInput?.Trim() ?? string.Empty;

        if (trimmedInput.Length == 0)
        {
            return NumericInputValidationResult<decimal>.Rejected("A value is required.");
        }

        if (!decimal.TryParse(trimmedInput, NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
                formatProvider ?? CultureInfo.InvariantCulture, out decimal parsedValue))
        {
            // decimal.TryParse also fails when the number exceeds decimal.MaxValue (~7.9e28).
            return NumericInputValidationResult<decimal>.Rejected(
                $"'{trimmedInput}' is not a number or is outside decimal limits ({decimal.MinValue:E2} to {decimal.MaxValue:E2}).");
        }

        return parsedValue switch
        {
            _ when parsedValue < minimumAllowedValue =>
                NumericInputValidationResult<decimal>.Rejected($"Value must be at least {minimumAllowedValue:N2}."),
            _ when parsedValue > maximumAllowedValue =>
                NumericInputValidationResult<decimal>.Rejected($"Value must be at most {maximumAllowedValue:N2}."),
            _ => NumericInputValidationResult<decimal>.Accepted(parsedValue)
        };
    }

    public static NumericInputValidationResult<int> ValidateIntegerWithinInclusiveRange(
        string? rawInput,
        int minimumAllowedValue,
        int maximumAllowedValue)
    {
        string trimmedInput = rawInput?.Trim() ?? string.Empty;

        if (trimmedInput.Length == 0)
        {
            return NumericInputValidationResult<int>.Rejected("A value is required.");
        }

        // Parse as long first so that "3000000000" is reported as "too large" rather than "not a number".
        if (!long.TryParse(trimmedInput, NumberStyles.Integer, CultureInfo.InvariantCulture, out long widenedValue))
        {
            return NumericInputValidationResult<int>.Rejected($"'{trimmedInput}' is not a whole number.");
        }

        if (widenedValue is < int.MinValue or > int.MaxValue)
        {
            return NumericInputValidationResult<int>.Rejected(
                $"Value overflows a 32-bit integer (allowed {int.MinValue:N0} to {int.MaxValue:N0}).");
        }

        // Relational pattern matching keeps the bound checks declarative.
        return (int)widenedValue switch
        {
            int value when value < minimumAllowedValue =>
                NumericInputValidationResult<int>.Rejected($"Value must be at least {minimumAllowedValue}."),
            int value when value > maximumAllowedValue =>
                NumericInputValidationResult<int>.Rejected($"Value must be at most {maximumAllowedValue}."),
            int value => NumericInputValidationResult<int>.Accepted(value)
        };
    }
}
