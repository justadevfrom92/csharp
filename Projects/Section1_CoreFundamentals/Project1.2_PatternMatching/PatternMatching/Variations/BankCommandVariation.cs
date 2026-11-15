using System.Globalization;

namespace PatternMatching.Variations;

// The same commands as BankCommandParser (LIST patterns like ["deposit", var amount]), written with
// Length checks and indexes. Same answers; the list-pattern version shows the SHAPE of valid input directly.
public static class BankCommandVariation
{
    public static string WithLengthAndIndex(string[]? words)
    {
        ArgumentNullException.ThrowIfNull(words);
        if (words.Length == 0)
        {
            throw new ArgumentException("No command given.", nameof(words));
        }
        string command = words[0];
        if (words.Length == 1 && command == "help")
        {
            return "Commands: balance | deposit <amount> | withdraw <amount>";
        }
        if (words.Length == 1 && command == "balance")
        {
            return "Showing balance";
        }
        if (command == "deposit" || command == "withdraw")
        {
            if (words.Length == 1)
            {
                throw new ArgumentException("Missing amount, e.g. 'deposit 25'.", nameof(words));
            }
            if (words.Length == 2)
            {
                if (!decimal.TryParse(words[1], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount) || amount <= 0)
                {
                    throw new InvalidOperationException($"'{words[1]}' is not an amount more than 0.");
                }
                return command == "deposit" ? $"Depositing {amount:0.00}" : $"Withdrawing {amount:0.00}";
            }
        }
        throw new ArgumentException($"Unknown command '{command}'.", nameof(words));
    }
}
