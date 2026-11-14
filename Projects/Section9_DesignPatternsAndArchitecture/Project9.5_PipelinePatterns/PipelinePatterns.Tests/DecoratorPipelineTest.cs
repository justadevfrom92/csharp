using PipelinePatterns.Decorators;
using PipelinePatterns.Models;
using PipelinePatterns.Processors;

namespace PipelinePatterns.Tests;

public class DecoratorPipelineTest
{
    private readonly List<string> log = new();

    private IPaymentProcessor Pipeline(FlakyPaymentProcessor bank, int attempts)
    {
        return new LoggingPaymentProcessorDecorator(
            new ValidationPaymentProcessorDecorator(
                new RetryPaymentProcessorDecorator(bank, attempts)),
            log);
    }

    [Fact]
    public void Process_PositivePositive_FailuresWithinRetryLimit_Succeed()
    {
        var bank = new FlakyPaymentProcessor(failuresBeforeSuccess: 2);

        string result = Pipeline(bank, attempts: 3).Process(new PaymentModel("A-1", 49.99m));

        Assert.Equal("paid 49.99 for A-1", result);
        Assert.Equal(3, bank.Calls);
        Assert.Equal(["start A-1", "ok paid 49.99 for A-1"], log);
    }

    [Fact]
    public void Process_PositiveNegative_InvalidAmount_StoppedBeforeTheBank_AndTooManyFailuresGiveUp()
    {
        var bank = new FlakyPaymentProcessor(failuresBeforeSuccess: 5);

        Assert.Throws<ArgumentOutOfRangeException>(() => Pipeline(bank, 3).Process(new PaymentModel("A-1", 20_000m)));
        Assert.Equal(0, bank.Calls); // validation sits OUTSIDE retry, so the bank is never called

        Assert.Throws<TimeoutException>(() => Pipeline(bank, 3).Process(new PaymentModel("A-2", 10m)));
        Assert.Equal(3, bank.Calls);
        Assert.Equal("failed TimeoutException", log[^1]);
    }

    [Fact]
    public void Process_NegativeNegative_NullPaymentOrZeroAttempts_Throws()
    {
        var bank = new FlakyPaymentProcessor(0);

        Assert.Throws<ArgumentNullException>(() => Pipeline(bank, 1).Process(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RetryPaymentProcessorDecorator(bank, 0));
        Assert.Equal(0, bank.Calls);
    }
}
