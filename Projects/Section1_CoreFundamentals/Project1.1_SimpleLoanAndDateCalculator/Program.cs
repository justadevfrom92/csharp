using System.Globalization;
using System.Text;

// Run with "--demo" to feed in ready-made answers instead of typing them.
Queue<string>? demoAnswers = args.Contains("--demo")
    ? new Queue<string>(["abc", "50", "20000", "6", "999", "12", "15/01/2027", ""])
    : null;

Console.WriteLine("=== Loan & Date Calculator ===");
Console.WriteLine();

// ---------- 1. Number limits ----------
Console.WriteLine($"int     range: {int.MinValue} to {int.MaxValue}");
Console.WriteLine($"double  range: {double.MinValue} to {double.MaxValue}");
Console.WriteLine($"decimal range: {decimal.MinValue} to {decimal.MaxValue}");

int biggestInt = int.MaxValue;
Console.WriteLine($"unchecked int.MaxValue + 1 = {unchecked(biggestInt + 1)}  (wraps around silently)");
try
{
    Console.WriteLine(checked(biggestInt + 1));
}
catch (OverflowException)
{
    Console.WriteLine("checked   int.MaxValue + 1 = OverflowException  (caught the problem)");
}

Console.WriteLine($"double  0.1 + 0.2 = {0.1 + 0.2:R}");
Console.WriteLine($"decimal 0.1 + 0.2 = {0.1m + 0.2m}");
Console.WriteLine();

// ---------- 2. Read loan input ----------
decimal loanAmount = AskForDecimal("Loan amount (100 to 10,000,000): ", 100m, 10_000_000m);
decimal yearlyInterestPercent = AskForDecimal("Yearly interest % (0 to 50): ", 0m, 50m);
int numberOfMonths = (int)AskForDecimal("Number of months (1 to 480): ", 1m, 480m);
DateOnly firstPaymentDate = AskForDate("First payment date (any format, e.g. 2027-01-15 or 15/01/2027): ");

// Name is optional.
//   "?."  -> only calls Trim() if the answer is not null
//   "??=" -> sets a default only if the value is still null
string? customerName = ReadAnswer("Your name (optional, press Enter to skip): ")?.Trim();
if (customerName == "")
{
    customerName = null;
}
customerName ??= "Customer";

// Nullable value type (int? is short for Nullable<int>). "??" picks 0 when there is no value.
int? extraMonthsOfGrace = null;
Console.WriteLine($"Grace months: {extraMonthsOfGrace ?? 0}");

// ---------- 3. Work out the monthly payment ----------
decimal monthlyInterestRate = yearlyInterestPercent / 100m / 12m;
decimal monthlyPayment;
if (monthlyInterestRate == 0)
{
    monthlyPayment = loanAmount / numberOfMonths;
}
else
{
    // Standard fixed-payment loan formula.
    decimal growthFactor = 1m;
    for (int month = 0; month < numberOfMonths; month++)
    {
        growthFactor *= 1m + monthlyInterestRate;
    }
    monthlyPayment = loanAmount * monthlyInterestRate * growthFactor / (growthFactor - 1m);
}
monthlyPayment = Math.Round(monthlyPayment, 2);

// ---------- 4. Build the receipt ----------
CultureInfo usCulture = CultureInfo.GetCultureInfo("en-US");
var receipt = new StringBuilder();
receipt.AppendLine();
receipt.AppendLine("================ LOAN RECEIPT ================");
receipt.AppendLine($"Customer:        {customerName}");
receipt.AppendLine($"Loan amount:     {loanAmount.ToString("C", usCulture)}");
receipt.AppendLine($"Yearly interest: {yearlyInterestPercent}%");
receipt.AppendLine($"Months:          {numberOfMonths}");
receipt.AppendLine($"Monthly payment: {monthlyPayment.ToString("C", usCulture)}");
receipt.AppendLine();
receipt.AppendLine($"{"#",4} {"Date",-12} {"Payment",12} {"Interest",10} {"Left to pay",14}");

decimal amountLeft = loanAmount;
decimal totalInterestPaid = 0m;
int monthNumber = 1;
while (amountLeft > 0)
{
    decimal interestThisMonth = Math.Round(amountLeft * monthlyInterestRate, 2);
    decimal paymentThisMonth = monthNumber == numberOfMonths
        ? amountLeft + interestThisMonth // last month pays whatever is left (fixes rounding cents)
        : monthlyPayment;

    amountLeft -= paymentThisMonth - interestThisMonth;
    totalInterestPaid += interestThisMonth;

    DateOnly paymentDate = firstPaymentDate.AddMonths(monthNumber - 1);
    receipt.AppendLine($"{monthNumber,4} {paymentDate:yyyy-MM-dd}   {paymentThisMonth,12:N2} {interestThisMonth,10:N2} {amountLeft,14:N2}");
    monthNumber++;
}

receipt.AppendLine();
receipt.AppendLine($"Total interest:  {totalInterestPaid.ToString("C", usCulture)}");
receipt.AppendLine($"Total paid:      {(loanAmount + totalInterestPaid).ToString("C", usCulture)}");
receipt.AppendLine($"Last payment on: {firstPaymentDate.AddMonths(numberOfMonths - 1):dddd, d MMMM yyyy}");
Console.Write(receipt.ToString());

// ---------- 5. Same date and price shown in different cultures ----------
Console.WriteLine();
Console.WriteLine("Same date and amount written the way other countries write them (no currency conversion):");
foreach (string cultureName in new[] { "en-US", "en-GB", "de-DE", "ja-JP" })
{
    CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
    Console.WriteLine($"  {cultureName}: {firstPaymentDate.ToString("d", culture),-12} {monthlyPayment.ToString("N2", culture)}");
}

// ---------- 6. Time zones ----------
DateTimeOffset receiptPrintedAt = DateTimeOffset.UtcNow;
Console.WriteLine();
Console.WriteLine("Receipt printed at:");
foreach (string timeZoneId in new[] { "UTC", "America/New_York", "Europe/London", "Asia/Tokyo" })
{
    DateTimeOffset localTime = TimeZoneInfo.ConvertTime(receiptPrintedAt, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));
    DateOnly localDate = DateOnly.FromDateTime(localTime.DateTime);
    TimeOnly localClock = TimeOnly.FromDateTime(localTime.DateTime);
    Console.WriteLine($"  {timeZoneId,-18} {localDate:yyyy-MM-dd} {localClock:HH:mm}  (UTC{localTime:zzz})");
}

TimeSpan timeUntilFirstPayment = firstPaymentDate.ToDateTime(TimeOnly.MinValue) - DateTime.Today;
Console.WriteLine($"Days until first payment: {timeUntilFirstPayment.Days}");
return;

// ================= Helpers =================

string? ReadAnswer(string question)
{
    Console.Write(question);
    if (demoAnswers is null)
    {
        return Console.ReadLine(); // null if input is closed
    }

    string? answer = demoAnswers.Count > 0 ? demoAnswers.Dequeue() : null;
    Console.WriteLine(answer);
    return answer;
}

decimal AskForDecimal(string question, decimal minimum, decimal maximum)
{
    while (true)
    {
        string answer = ReadAnswer(question) ?? throw new InvalidOperationException("No more input.");

        if (!decimal.TryParse(answer, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal number))
        {
            Console.WriteLine("  Not a number, try again.");
        }
        else if (number < minimum || number > maximum)
        {
            Console.WriteLine($"  Must be between {minimum} and {maximum}, try again.");
        }
        else
        {
            return number;
        }
    }
}

DateOnly AskForDate(string question)
{
    string[] culturesToTry = ["en-US", "en-GB", "de-DE"];
    while (true)
    {
        string answer = ReadAnswer(question) ?? throw new InvalidOperationException("No more input.");

        // Try the unambiguous ISO format first, then each culture in order.
        if (DateOnly.TryParseExact(answer, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly isoDate))
        {
            return isoDate;
        }
        foreach (string cultureName in culturesToTry)
        {
            if (DateOnly.TryParse(answer, CultureInfo.GetCultureInfo(cultureName), DateTimeStyles.None, out DateOnly date))
            {
                Console.WriteLine($"  Read as {cultureName} date: {date:yyyy-MM-dd}");
                return date;
            }
        }
        Console.WriteLine("  Could not read that date, try again.");
    }
}
