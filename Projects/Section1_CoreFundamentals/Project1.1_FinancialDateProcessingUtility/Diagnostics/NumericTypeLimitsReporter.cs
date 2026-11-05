using System.Text;

namespace FinancialDateProcessingUtility.Diagnostics;

/// <summary>
/// Prints the representable range of each built-in numeric type and demonstrates
/// checked vs unchecked overflow behaviour and the double-vs-decimal precision difference.
/// </summary>
public static class NumericTypeLimitsReporter
{
    public static string BuildNumericLimitsReport()
    {
        var reportBuilder = new StringBuilder();
        reportBuilder.AppendLine("Type       Minimum                                   Maximum");
        reportBuilder.AppendLine($"byte       {byte.MinValue,-41} {byte.MaxValue}");
        reportBuilder.AppendLine($"short      {short.MinValue,-41} {short.MaxValue}");
        reportBuilder.AppendLine($"int        {int.MinValue,-41} {int.MaxValue}");
        reportBuilder.AppendLine($"long       {long.MinValue,-41} {long.MaxValue}");
        reportBuilder.AppendLine($"float      {float.MinValue,-41} {float.MaxValue}");
        reportBuilder.AppendLine($"double     {double.MinValue,-41} {double.MaxValue}");
        reportBuilder.AppendLine($"decimal    {decimal.MinValue,-41} {decimal.MaxValue}");
        reportBuilder.AppendLine($"char       U+{(int)char.MinValue:X4}{"",-35} U+{(int)char.MaxValue:X4}");
        reportBuilder.AppendLine($"bool       {false,-41} {true}");
        reportBuilder.AppendLine();

        int largestInteger = int.MaxValue;
        int wrappedValue = unchecked(largestInteger + 1);
        reportBuilder.AppendLine($"unchecked(int.MaxValue + 1) = {wrappedValue}  (silent wrap-around)");

        try
        {
            int overflowingValue = checked(largestInteger + 1);
            reportBuilder.AppendLine($"checked result: {overflowingValue}");
        }
        catch (OverflowException overflowException)
        {
            reportBuilder.AppendLine($"checked(int.MaxValue + 1)   -> {overflowException.GetType().Name}: {overflowException.Message}");
        }

        double binaryFloatingSum = 0.1 + 0.2;
        decimal baseTenSum = 0.1m + 0.2m;
        reportBuilder.AppendLine($"double  0.1 + 0.2 = {binaryFloatingSum:R}  (== 0.3? {binaryFloatingSum == 0.3})");
        reportBuilder.AppendLine($"decimal 0.1 + 0.2 = {baseTenSum}                (== 0.3? {baseTenSum == 0.3m})");

        double accumulatedDoublePennies = 0;
        decimal accumulatedDecimalPennies = 0;
        for (int iteration = 0; iteration < 1_000_000; iteration++)
        {
            accumulatedDoublePennies += 0.01;
            accumulatedDecimalPennies += 0.01m;
        }
        reportBuilder.AppendLine($"1,000,000 x 0.01 as double  = {accumulatedDoublePennies:R}");
        reportBuilder.AppendLine($"1,000,000 x 0.01 as decimal = {accumulatedDecimalPennies}");
        return reportBuilder.ToString();
    }
}
