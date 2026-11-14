using System.Globalization;
using System.Text.RegularExpressions;
using FluentBuilderAndDsl.Models;

namespace FluentBuilderAndDsl.Parsers;

// An EXTERNAL DSL (Domain-Specific Language): a tiny language made for ONE job - pricing rules -
// that a non-programmer can read and edit in a text file:
//
//   # comments start with #
//   WHEN total > 100 THEN discount 10%
//   WHEN items >= 5 THEN discount 5%
//   WHEN country = CA THEN shipping 4.99
//
// Grammar (what a valid line looks like):
//   WHEN <field> <operator> <value> THEN discount <whole number>%
//   WHEN <field> <operator> <value> THEN shipping <amount>
//   field:    total | items | country
//   operator: >  >=  <  <=  =      (country only allows =)
public static class PricingRuleParser
{
    private static readonly Regex linePattern = new Regex(
        @"^WHEN\s+(?<field>\w+)\s*(?<operator>>=|<=|>|<|=)\s*(?<value>\S+)\s+THEN\s+(?<action>\w+)\s+(?<amount>[0-9.]+)(?<percent>%?)$",
        RegexOptions.IgnoreCase);

    private static readonly string[] fields = ["total", "items", "country"];

    public static List<PricingRuleModel> Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var rules = new List<PricingRuleModel>();
        string[] lines = text.Split('\n');
        for (int index = 0; index < lines.Length; index++)
        {
            int lineNumber = index + 1;
            string line = lines[index].Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }
            rules.Add(ParseLine(line, lineNumber));
        }
        return rules;
    }

    private static PricingRuleModel ParseLine(string line, int lineNumber)
    {
        Match match = linePattern.Match(line);
        if (!match.Success)
        {
            throw Error(lineNumber, $"Expected 'WHEN <field> <operator> <value> THEN <action> <amount>', got '{line}'.");
        }

        string field = match.Groups["field"].Value.ToLowerInvariant();
        string comparison = match.Groups["operator"].Value;
        string value = match.Groups["value"].Value;
        string action = match.Groups["action"].Value.ToLowerInvariant();
        bool hasPercent = match.Groups["percent"].Value == "%";

        if (!fields.Contains(field))
        {
            throw Error(lineNumber, $"Unknown field '{field}'. Use total, items or country.");
        }
        if (field == "country" && comparison != "=")
        {
            throw Error(lineNumber, "country can only be compared with '='.");
        }
        if (field != "country" && !decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
        {
            throw Error(lineNumber, $"'{value}' is not a number.");
        }
        if (!decimal.TryParse(match.Groups["amount"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount))
        {
            throw Error(lineNumber, $"'{match.Groups["amount"].Value}' is not a number.");
        }

        if (action == "discount")
        {
            if (!hasPercent || amount != decimal.Truncate(amount) || amount < 1 || amount > 50)
            {
                throw Error(lineNumber, "A discount is a whole percentage from 1% to 50%, e.g. 'discount 10%'.");
            }
        }
        else if (action == "shipping")
        {
            if (hasPercent)
            {
                throw Error(lineNumber, "Shipping is an amount, not a percentage.");
            }
        }
        else
        {
            throw Error(lineNumber, $"Unknown action '{action}'. Use discount or shipping.");
        }

        return new PricingRuleModel(lineNumber, field, comparison, value.ToUpperInvariant(), action, amount);
    }

    // The error says WHICH LINE is wrong - essential for anyone editing the rules file.
    private static FormatException Error(int lineNumber, string message)
    {
        return new FormatException($"Line {lineNumber}: {message}");
    }
}
