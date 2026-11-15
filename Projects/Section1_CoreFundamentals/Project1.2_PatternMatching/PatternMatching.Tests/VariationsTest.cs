using PatternMatching.Classifiers;
using PatternMatching.Models;
using PatternMatching.Parsers;
using PatternMatching.Variations;

namespace PatternMatching.Tests;

// Each variation is compared with the ORIGINAL: same answers where they should be the same,
// and the one place they DIFFER is pinned down.
// PositivePositive = valid input -> same result; PositiveNegative = valid input where a variation behaves differently;
// NegativeNegative = broken input -> rejected the same way.
public class VariationsTest
{
    // ---------- credit score ----------

    [Fact]
    public void CreditScore_PositivePositive_EveryValidScore_AllFourWaysAgree()
    {
        for (int score = CreditScoreClassifier.LowestScore; score <= CreditScoreClassifier.HighestScore; score++)
        {
            string expected = CreditScoreClassifier.Classify(score);
            Assert.Equal(expected, CreditScoreClassifier.ClassifyWithIfStatements(score));
            Assert.Equal(expected, CreditScoreVariation.WithSwitchStatement(score));
            Assert.Equal(expected, CreditScoreVariation.WithLookupTable(score));
            Assert.Equal(expected, CreditScoreVariation.WithTernaryChain(score));
        }
    }

    [Theory]
    [InlineData(579, "Poor")]
    [InlineData(580, "Fair")]
    [InlineData(799, "Very good")]
    [InlineData(800, "Excellent")]
    public void CreditScore_PositiveNegative_BandEdges_AreTheSameInEveryVersion(int score, string expected)
    {
        Assert.Equal(expected, CreditScoreVariation.WithLookupTable(score));
        Assert.Equal(expected, CreditScoreVariation.WithSwitchStatement(score));
    }

    [Theory]
    [InlineData(299)]
    [InlineData(851)]
    [InlineData(int.MinValue)]
    public void CreditScore_NegativeNegative_OutOfRange_EveryVersionThrows(int score)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreditScoreVariation.WithSwitchStatement(score));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreditScoreVariation.WithLookupTable(score));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreditScoreVariation.WithTernaryChain(score));
    }

    // ---------- type checks ----------

    [Fact]
    public void TypeCheck_PositivePositive_CardPayment_AllFourFindTheAmount()
    {
        var card = new CardPaymentModel(25m, "1234");

        Assert.Equal(25m, PaymentTypeCheckVariation.CardAmountWithIsPattern(card));
        Assert.Equal(25m, PaymentTypeCheckVariation.CardAmountWithAs(card));
        Assert.Equal(25m, PaymentTypeCheckVariation.CardAmountWithCast(card));
        Assert.Equal(25m, PaymentTypeCheckVariation.CardAmountWithExactType(card));
    }

    [Fact]
    public void TypeCheck_PositiveNegative_ChildType_OnlyExactTypeCheckSaysNo()
    {
        var giftCard = new GiftCardPaymentVariation(25m, "1234", "GIFT-1");

        Assert.Equal(25m, PaymentTypeCheckVariation.CardAmountWithIsPattern(giftCard));
        Assert.Equal(25m, PaymentTypeCheckVariation.CardAmountWithAs(giftCard));
        Assert.Equal(25m, PaymentTypeCheckVariation.CardAmountWithCast(giftCard));
        Assert.Null(PaymentTypeCheckVariation.CardAmountWithExactType(giftCard)); // GetType() == typeof(...) ignores inheritance
        Assert.StartsWith("Card payment", PaymentClassifier.Describe(giftCard));   // the original's type pattern matches it too
    }

    [Fact]
    public void TypeCheck_NegativeNegative_NullOrOtherType_AllReturnNull()
    {
        foreach (object? payment in new object?[] { null, new CashPaymentModel(10m), "not a payment" })
        {
            Assert.Null(PaymentTypeCheckVariation.CardAmountWithIsPattern(payment));
            Assert.Null(PaymentTypeCheckVariation.CardAmountWithAs(payment));
            Assert.Null(PaymentTypeCheckVariation.CardAmountWithCast(payment));
            Assert.Null(PaymentTypeCheckVariation.CardAmountWithExactType(payment));
        }
    }

    // ---------- shipping ----------

    [Theory]
    [InlineData(0.5, "US", false)]
    [InlineData(1, "US", false)]
    [InlineData(5, "US", false)]
    [InlineData(5, "US", true)]
    [InlineData(5, "GB", true)]
    [InlineData(30, "GB", false)]
    public void Shipping_PositivePositive_NestedIfsGiveTheSamePrice(decimal weight, string country, bool express)
    {
        var parcel = new ParcelModel(weight, country, express);

        Assert.Equal(ShippingCostClassifier.CalculateCost(parcel), ShippingCostVariation.WithNestedIfs(parcel));
    }

    [Fact]
    public void Shipping_PositiveNegative_TooHeavy_BothRefuseTheSameWay()
    {
        var parcel = new ParcelModel(31m, "US", false);

        Assert.Throws<InvalidOperationException>(() => ShippingCostClassifier.CalculateCost(parcel));
        Assert.Throws<InvalidOperationException>(() => ShippingCostVariation.WithNestedIfs(parcel));
    }

    [Fact]
    public void Shipping_NegativeNegative_NullOrZeroWeight_BothThrow()
    {
        Assert.Throws<ArgumentNullException>(() => ShippingCostVariation.WithNestedIfs(null));
        Assert.Throws<ArgumentException>(() => ShippingCostVariation.WithNestedIfs(new ParcelModel(0m, "US", false)));
    }

    // ---------- bank commands ----------

    [Theory]
    [InlineData("help")]
    [InlineData("balance")]
    [InlineData("deposit 25")]
    [InlineData("withdraw 10.5")]
    public void Command_PositivePositive_LengthAndIndexGiveTheSameAnswer(string input)
    {
        string[] words = input.Split(' ');

        Assert.Equal(BankCommandParser.Parse(words), BankCommandVariation.WithLengthAndIndex(words));
    }

    [Theory]
    [InlineData("deposit")]
    [InlineData("deposit -5")]
    [InlineData("transfer 5")]
    public void Command_PositiveNegative_RuleBroken_SameExceptionType(string input)
    {
        string[] words = input.Split(' ');
        Exception original = Record.Exception(() => BankCommandParser.Parse(words))!;
        Exception variation = Record.Exception(() => BankCommandVariation.WithLengthAndIndex(words))!;

        Assert.Equal(original.GetType(), variation.GetType());
    }

    [Fact]
    public void Command_NegativeNegative_NullOrEmpty_BothThrow()
    {
        Assert.Throws<ArgumentNullException>(() => BankCommandVariation.WithLengthAndIndex(null));
        Assert.Throws<ArgumentException>(() => BankCommandVariation.WithLengthAndIndex([]));
    }
}
