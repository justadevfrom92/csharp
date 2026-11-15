using PatternMatching.Classifiers;

namespace PatternMatching.Variations;

// The same credit-score rules as CreditScoreClassifier.Classify (switch EXPRESSION), written three other ways.
// (A fourth, plain if statements, is CreditScoreClassifier.ClassifyWithIfStatements.)
public static class CreditScoreVariation
{
    // Classic switch STATEMENT with "case ... when": each case needs "return" (or "break").
    // Older style; the switch EXPRESSION is shorter and the compiler warns if a value isn't covered.
    public static string WithSwitchStatement(int creditScore)
    {
        switch (creditScore)
        {
            case < CreditScoreClassifier.LowestScore:
            case > CreditScoreClassifier.HighestScore:
                throw new ArgumentOutOfRangeException(nameof(creditScore));
            case int score when score < 580:
                return "Poor";
            case < 670:
                return "Fair";
            case < 740:
                return "Good";
            case < 800:
                return "Very good";
            default:
                return "Excellent";
        }
    }

    // DATA-DRIVEN: the limits are a TABLE, not code. Change a limit = change data (could come from a settings file).
    private static readonly (int UpTo, string Label)[] bands =
    [
        (579, "Poor"),
        (669, "Fair"),
        (739, "Good"),
        (799, "Very good"),
        (CreditScoreClassifier.HighestScore, "Excellent"),
    ];

    public static string WithLookupTable(int creditScore)
    {
        if (creditScore < CreditScoreClassifier.LowestScore || creditScore > CreditScoreClassifier.HighestScore)
        {
            throw new ArgumentOutOfRangeException(nameof(creditScore));
        }
        return bands.First(band => creditScore <= band.UpTo).Label;
    }

    // Chained ternary ?: - compact but hard to read past 2-3 levels. Shown so you recognise it, not to copy it.
    public static string WithTernaryChain(int creditScore)
    {
        return creditScore < CreditScoreClassifier.LowestScore || creditScore > CreditScoreClassifier.HighestScore
            ? throw new ArgumentOutOfRangeException(nameof(creditScore))
            : creditScore < 580 ? "Poor"
            : creditScore < 670 ? "Fair"
            : creditScore < 740 ? "Good"
            : creditScore < 800 ? "Very good"
            : "Excellent";
    }
}
