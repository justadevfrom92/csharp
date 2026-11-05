using System.Globalization;
using System.Text;
using FinancialDateProcessingUtility.Calculation;

namespace FinancialDateProcessingUtility.Formatting;

/// <summary>
/// Builds a printable receipt. A <see cref="StringBuilder"/> is used because a 360-row schedule
/// concatenated with '+' would allocate 360+ intermediate strings; StringBuilder appends into one
/// growable buffer.
/// </summary>
public static class PaymentReceiptTextFormatter
{
    private const int ReceiptWidthInCharacters = 78;

    public static string FormatAmortizationReceipt(
        FixedRateLoanTerms loanTerms,
        LoanAmortizationSummary amortizationSummary,
        CultureInfo displayCulture,
        string? customerDisplayName,
        int maximumScheduleRowsToPrint = 12)
    {
        // Null-coalescing: an anonymous customer still gets a readable receipt.
        string customerLabel = customerDisplayName ?? "Valued Customer";
        var receiptBuilder = new StringBuilder(capacity: 4096);

        receiptBuilder.AppendLine(new string('=', ReceiptWidthInCharacters));
        receiptBuilder.AppendLine(CenterText("LOAN PAYMENT RECEIPT", ReceiptWidthInCharacters));
        receiptBuilder.AppendLine(new string('=', ReceiptWidthInCharacters));
        receiptBuilder.AppendLine(string.Create(displayCulture, $"Customer        : {customerLabel}"));
        receiptBuilder.AppendLine(string.Create(displayCulture, $"Principal       : {loanTerms.PrincipalAmount:C2}"));
        receiptBuilder.AppendLine(string.Create(displayCulture, $"Annual rate     : {loanTerms.AnnualInterestRatePercent / 100m:P3}"));
        receiptBuilder.AppendLine(string.Create(displayCulture, $"Term            : {loanTerms.NumberOfMonthlyPayments} months"));
        receiptBuilder.AppendLine(string.Create(displayCulture, $"First payment   : {loanTerms.FirstPaymentDate:D}"));
        receiptBuilder.AppendLine(string.Create(displayCulture, $"Monthly payment : {amortizationSummary.StandardMonthlyPaymentAmount:C2}"));
        receiptBuilder.AppendLine(string.Create(displayCulture, $"Total interest  : {amortizationSummary.TotalInterestPaid:C2}"));
        receiptBuilder.AppendLine(string.Create(displayCulture, $"Total paid      : {amortizationSummary.TotalAmountPaid:C2}"));
        receiptBuilder.AppendLine(new string('-', ReceiptWidthInCharacters));
        receiptBuilder.AppendLine($"{"#",4} {"Due date",-12} {"Payment",14} {"Interest",14} {"Principal",14} {"Balance",14}");

        IEnumerable<MonthlyAmortizationScheduleEntry> printedEntries = SelectHeadAndTailEntries(
            amortizationSummary.ScheduleEntries, maximumScheduleRowsToPrint);

        int? previousPaymentNumber = null;
        foreach (MonthlyAmortizationScheduleEntry scheduleEntry in printedEntries)
        {
            if (previousPaymentNumber is int previous && scheduleEntry.PaymentNumber != previous + 1)
            {
                receiptBuilder.AppendLine(CenterText("...", ReceiptWidthInCharacters));
            }

            receiptBuilder.AppendLine(string.Create(displayCulture,
                $"{scheduleEntry.PaymentNumber,4} {scheduleEntry.PaymentDueDate.ToString("d", displayCulture),-12} {scheduleEntry.TotalPaymentAmount,14:N2} {scheduleEntry.InterestPortion,14:N2} {scheduleEntry.PrincipalPortion,14:N2} {scheduleEntry.RemainingBalanceAfterPayment,14:N2}"));
            previousPaymentNumber = scheduleEntry.PaymentNumber;
        }

        receiptBuilder.AppendLine(new string('=', ReceiptWidthInCharacters));
        receiptBuilder.AppendLine(string.Create(displayCulture,
            $"Issued {TimeOnly.FromDateTime(DateTime.Now):t} on {DateOnly.FromDateTime(DateTime.Now):d} ({displayCulture.Name})"));
        return receiptBuilder.ToString();
    }

    private static IEnumerable<MonthlyAmortizationScheduleEntry> SelectHeadAndTailEntries(
        IReadOnlyList<MonthlyAmortizationScheduleEntry> allEntries, int maximumRows)
    {
        if (allEntries.Count <= maximumRows)
        {
            return allEntries;
        }
        int headCount = maximumRows / 2;
        int tailCount = maximumRows - headCount;
        return allEntries.Take(headCount).Concat(allEntries.Skip(allEntries.Count - tailCount));
    }

    private static string CenterText(string text, int totalWidth)
    {
        int leftPadding = Math.Max(0, (totalWidth - text.Length) / 2);
        return text.PadLeft(leftPadding + text.Length);
    }
}
