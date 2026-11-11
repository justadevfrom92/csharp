using System.Globalization;

namespace PatternMatching.Parsers;

public static class BankCommandParser
{
    // LIST patterns: [first, second] matches an array of exactly 2 items; ".." means "any number of items".
    // "var amount" captures an item into a variable.
    public static string Parse(string[]? words)
    {
        return words switch
        {
            null => throw new ArgumentNullException(nameof(words)),
            [] => throw new ArgumentException("No command given.", nameof(words)),
            ["help"] => "Commands: balance | deposit <amount> | withdraw <amount>",
            ["balance"] => "Showing balance",
            ["deposit" or "withdraw"] => throw new ArgumentException("Missing amount, e.g. 'deposit 25'.", nameof(words)),
            ["deposit" or "withdraw", var amountText] when !IsPositiveAmount(amountText) => throw new InvalidOperationException($"'{amountText}' is not an amount more than 0."),
            ["deposit", var amountText] => $"Depositing {decimal.Parse(amountText, CultureInfo.InvariantCulture):0.00}",
            ["withdraw", var amountText] => $"Withdrawing {decimal.Parse(amountText, CultureInfo.InvariantCulture):0.00}",
            [var unknownCommand, ..] => throw new ArgumentException($"Unknown command '{unknownCommand}'.", nameof(words)),
        };
    }

    private static bool IsPositiveAmount(string text)
    {
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount) && amount > 0;
    }
}
