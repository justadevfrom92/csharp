namespace Roadmap.SimpleHelpers.Validators;

/// <summary>Checks card numbers for typos (does NOT check the card exists).</summary>
public static class PaymentCardNumberValidator
{
    /// <summary>True if the number passes the Luhn check. Spaces and dashes are ignored.</summary>
    public static bool IsValidCardNumber(string cardNumber)
    {
        string digits = new string(cardNumber.Where(character => character != ' ' && character != '-').ToArray());
        if (digits.Length < 12 || digits.Length > 19 || !digits.All(char.IsAsciiDigit))
        {
            return false;
        }

        // Luhn: from the right, double every second digit; if that's over 9, subtract 9; the total must end in 0.
        int total = 0;
        for (int position = 0; position < digits.Length; position++)
        {
            int digit = digits[digits.Length - 1 - position] - '0';
            if (position % 2 == 1)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }
            total += digit;
        }
        return total % 10 == 0;
    }
}
