using PatternMatching.Classifiers;

namespace PatternMatching.Tests;

// PositivePositive - VALID input -> succeeds
// PositiveNegative - well-formed input that breaks a rule -> correctly refused
// NegativeNegative - malformed / impossible input -> rejected
public class CreditScoreClassifierTest
{
    [Theory]
    [InlineData(300, "Poor")]
    [InlineData(579, "Poor")]       // edge: last "Poor"
    [InlineData(580, "Fair")]       // edge: first "Fair"
    [InlineData(669, "Fair")]
    [InlineData(670, "Good")]
    [InlineData(739, "Good")]
    [InlineData(740, "Very good")]
    [InlineData(799, "Very good")]
    [InlineData(800, "Excellent")]
    [InlineData(850, "Excellent")]
    public void Classify_PositivePositive_ValidScore_ReturnsCategory(int score, string expected)
    {
        Assert.Equal(expected, CreditScoreClassifier.Classify(score));
        Assert.Equal(expected, CreditScoreClassifier.ClassifyWithIfStatements(score)); // both versions agree
    }

    [Theory]
    [InlineData(299)]
    [InlineData(851)]
    public void Classify_PositiveNegative_ScoreJustOutsideRange_IsRefused(int score)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreditScoreClassifier.Classify(score));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void Classify_NegativeNegative_ImpossibleScore_IsRejected(int score)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreditScoreClassifier.Classify(score));
    }
}
