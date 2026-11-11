namespace PatternMatching.Classifiers;

public static class CreditScoreClassifier
{
    public const int LowestScore = 300;
    public const int HighestScore = 850;

    // SWITCH EXPRESSION with RELATIONAL patterns (<, <=, >, >=) and LOGICAL patterns (and, or).
    // Each arm is "pattern => result". The arms are checked top to bottom; the first match wins.
    public static string Classify(int creditScore)
    {
        return creditScore switch
        {
            < LowestScore or > HighestScore => throw new ArgumentOutOfRangeException(nameof(creditScore), $"Score must be {LowestScore}-{HighestScore}."),
            < 580 => "Poor",
            >= 580 and < 670 => "Fair",   // "and": both must be true (the ">= 580" is only here to show "and")
            < 740 => "Good",
            < 800 => "Very good",
            _ => "Excellent",             // "_" (discard) = anything else
        };
    }

    // The same rules written the OLD way, for comparison: longer, and easy to get an edge wrong.
    public static string ClassifyWithIfStatements(int creditScore)
    {
        if (creditScore < LowestScore || creditScore > HighestScore)
        {
            throw new ArgumentOutOfRangeException(nameof(creditScore));
        }
        if (creditScore < 580)
        {
            return "Poor";
        }
        if (creditScore < 670)
        {
            return "Fair";
        }
        if (creditScore < 740)
        {
            return "Good";
        }
        if (creditScore < 800)
        {
            return "Very good";
        }
        return "Excellent";
    }
}
