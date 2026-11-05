namespace FinancialDateProcessingUtility.Calculation;

/// <summary>Immutable input describing a fixed-rate, fully amortizing loan.</summary>
public sealed record FixedRateLoanTerms(
    decimal PrincipalAmount,
    decimal AnnualInterestRatePercent,
    int NumberOfMonthlyPayments,
    DateOnly FirstPaymentDate);

/// <summary>One row of the amortization schedule.</summary>
public sealed record MonthlyAmortizationScheduleEntry(
    int PaymentNumber,
    DateOnly PaymentDueDate,
    decimal TotalPaymentAmount,
    decimal InterestPortion,
    decimal PrincipalPortion,
    decimal RemainingBalanceAfterPayment);

public sealed record LoanAmortizationSummary(
    decimal StandardMonthlyPaymentAmount,
    decimal TotalInterestPaid,
    decimal TotalAmountPaid,
    IReadOnlyList<MonthlyAmortizationScheduleEntry> ScheduleEntries);

/// <summary>
/// Calculates a month-by-month payment breakdown using <see cref="decimal"/> exclusively.
/// No value ever passes through <see cref="double"/>, so there is no binary floating-point
/// representation error (0.1 + 0.2 == 0.3 holds in decimal, not in double).
/// </summary>
public static class LoanAmortizationScheduleCalculator
{
    private const int MonthsPerYear = 12;
    private const int CurrencyDecimalPlaces = 2;

    public static LoanAmortizationSummary CalculateAmortizationSchedule(FixedRateLoanTerms loanTerms)
    {
        ArgumentNullException.ThrowIfNull(loanTerms);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(loanTerms.PrincipalAmount);
        ArgumentOutOfRangeException.ThrowIfNegative(loanTerms.AnnualInterestRatePercent);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(loanTerms.NumberOfMonthlyPayments);

        decimal monthlyInterestRate = loanTerms.AnnualInterestRatePercent / 100m / MonthsPerYear;
        decimal standardMonthlyPayment = CalculateStandardMonthlyPayment(
            loanTerms.PrincipalAmount, monthlyInterestRate, loanTerms.NumberOfMonthlyPayments);

        var scheduleEntries = new List<MonthlyAmortizationScheduleEntry>(loanTerms.NumberOfMonthlyPayments);
        decimal remainingBalance = loanTerms.PrincipalAmount;
        decimal accumulatedInterest = 0m;
        decimal accumulatedPayments = 0m;

        for (int paymentNumber = 1; paymentNumber <= loanTerms.NumberOfMonthlyPayments; paymentNumber++)
        {
            // checked: an overflow here would mean corrupted money, so fail loudly instead of wrapping.
            checked
            {
                decimal interestPortion = RoundCurrency(remainingBalance * monthlyInterestRate);
                bool isFinalPayment = paymentNumber == loanTerms.NumberOfMonthlyPayments;

                // The last payment absorbs the accumulated rounding pennies so the balance ends at exactly 0.00.
                decimal principalPortion = isFinalPayment
                    ? remainingBalance
                    : RoundCurrency(standardMonthlyPayment - interestPortion);

                decimal totalPayment = principalPortion + interestPortion;
                remainingBalance -= principalPortion;
                accumulatedInterest += interestPortion;
                accumulatedPayments += totalPayment;

                scheduleEntries.Add(new MonthlyAmortizationScheduleEntry(
                    paymentNumber,
                    loanTerms.FirstPaymentDate.AddMonths(paymentNumber - 1),
                    totalPayment,
                    interestPortion,
                    principalPortion,
                    remainingBalance));
            }
        }

        return new LoanAmortizationSummary(standardMonthlyPayment, accumulatedInterest, accumulatedPayments, scheduleEntries);
    }

    /// <summary>
    /// Standard annuity formula: P * r / (1 - (1 + r)^-n). The power is computed with repeated
    /// decimal multiplication because Math.Pow only exists for double.
    /// </summary>
    public static decimal CalculateStandardMonthlyPayment(decimal principalAmount, decimal monthlyInterestRate, int numberOfPayments)
    {
        if (monthlyInterestRate == 0m)
        {
            return RoundCurrency(principalAmount / numberOfPayments);
        }

        decimal growthFactor = RaiseDecimalToNonNegativeIntegerPower(1m + monthlyInterestRate, numberOfPayments);
        decimal payment = principalAmount * monthlyInterestRate * growthFactor / (growthFactor - 1m);
        return RoundCurrency(payment);
    }

    public static decimal RaiseDecimalToNonNegativeIntegerPower(decimal baseValue, int exponent)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(exponent);
        decimal result = 1m;
        decimal currentSquare = baseValue;
        int remainingExponent = exponent;

        // Exponentiation by squaring: O(log n) multiplications, which also limits rounding steps.
        while (remainingExponent > 0)
        {
            if ((remainingExponent & 1) == 1)
            {
                result *= currentSquare;
            }
            remainingExponent >>= 1;
            if (remainingExponent > 0)
            {
                currentSquare *= currentSquare;
            }
        }
        return result;
    }

    /// <summary>Banker's rounding is the .NET default; money uses AwayFromZero to match consumer expectations.</summary>
    private static decimal RoundCurrency(decimal amount) =>
        Math.Round(amount, CurrencyDecimalPlaces, MidpointRounding.AwayFromZero);
}
