using PatternMatching.Classifiers;
using PatternMatching.Models;

namespace PatternMatching.Tests;

public class PaymentClassifierTest
{
    // ---------- Describe ----------

    [Fact]
    public void Describe_PositivePositive_ValidPayments_AreDescribed()
    {
        Assert.Equal("Card payment 50.00 (card ...4242)", PaymentClassifier.Describe(new CardPaymentModel(50m, "4242")));
        Assert.Equal("Large card payment 2500.00 (card ...1111) - needs approval", PaymentClassifier.Describe(new CardPaymentModel(2500m, "1111")));
        Assert.Equal("Cash payment 20.00", PaymentClassifier.Describe(new CashPaymentModel(20m)));
    }

    [Fact]
    public void Describe_PositiveNegative_CashOverReportingLimit_IsRefused()
    {
        Assert.Throws<InvalidOperationException>(() => PaymentClassifier.Describe(new CashPaymentModel(20000m)));
    }

    [Fact]
    public void Describe_NegativeNegative_NullZeroOrUnknownType_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => PaymentClassifier.Describe(null));
        Assert.Throws<ArgumentException>(() => PaymentClassifier.Describe(new CardPaymentModel(0m, "4242")));
        Assert.Throws<NotSupportedException>(() => PaymentClassifier.Describe("not a payment"));
    }

    // ---------- NeedsApproval ----------

    [Fact]
    public void NeedsApproval_PositivePositive_LargePayments_NeedApproval()
    {
        Assert.True(PaymentClassifier.NeedsApproval(new CardPaymentModel(1000.01m, "4242")));
        Assert.True(PaymentClassifier.NeedsApproval(new CashPaymentModel(5000.01m)));
    }

    [Fact]
    public void NeedsApproval_PositiveNegative_SmallPayments_DoNotNeedApproval()
    {
        Assert.False(PaymentClassifier.NeedsApproval(new CardPaymentModel(1000m, "4242")));
        Assert.False(PaymentClassifier.NeedsApproval(new CashPaymentModel(5000m)));
    }

    [Fact]
    public void NeedsApproval_NegativeNegative_NullOrUnknownType_ReturnsFalse()
    {
        Assert.False(PaymentClassifier.NeedsApproval(null));
        Assert.False(PaymentClassifier.NeedsApproval(12345));
    }

    // ---------- HasUsableCardDigits ("not" pattern) ----------

    [Fact]
    public void HasUsableCardDigits_PositivePositive_FourRealDigits_ReturnsTrue()
    {
        Assert.True(PaymentClassifier.HasUsableCardDigits(new CardPaymentModel(10m, "4242")));
    }

    [Fact]
    public void HasUsableCardDigits_PositiveNegative_FourZeros_ReturnsFalse()
    {
        Assert.False(PaymentClassifier.HasUsableCardDigits(new CardPaymentModel(10m, "0000")));
    }

    [Theory]
    [InlineData("42")]
    [InlineData("")]
    [InlineData(null)]
    public void HasUsableCardDigits_NegativeNegative_WrongLengthOrMissing_ReturnsFalse(string? digits)
    {
        Assert.False(PaymentClassifier.HasUsableCardDigits(new CardPaymentModel(10m, digits!)));
        Assert.False(PaymentClassifier.HasUsableCardDigits(null));
    }
}
