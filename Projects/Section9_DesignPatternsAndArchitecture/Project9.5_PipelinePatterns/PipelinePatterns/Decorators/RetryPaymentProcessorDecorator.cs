using PipelinePatterns.Models;
using PipelinePatterns.Processors;

namespace PipelinePatterns.Decorators;

// Tries again on TimeoutException, up to "maximumAttempts" times in total.
public class RetryPaymentProcessorDecorator : IPaymentProcessor
{
    private readonly IPaymentProcessor inner;
    private readonly int maximumAttempts;

    public RetryPaymentProcessorDecorator(IPaymentProcessor inner, int maximumAttempts)
    {
        if (maximumAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAttempts), "Must be at least 1.");
        }
        this.inner = inner;
        this.maximumAttempts = maximumAttempts;
    }

    public string Process(PaymentModel payment)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                return inner.Process(payment);
            }
            catch (TimeoutException) when (attempt < maximumAttempts)
            {
                // try again (the last attempt's exception is NOT caught, so it reaches the caller)
            }
        }
    }
}
