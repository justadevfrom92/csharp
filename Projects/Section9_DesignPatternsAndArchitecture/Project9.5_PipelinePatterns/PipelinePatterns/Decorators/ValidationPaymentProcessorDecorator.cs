using PipelinePatterns.Models;
using PipelinePatterns.Processors;

namespace PipelinePatterns.Decorators;

// DECORATOR: implements the SAME interface and WRAPS another implementation, adding one extra behaviour.
// Stack several and you get a pipeline: each layer does its part, then calls the one inside.
public class ValidationPaymentProcessorDecorator : IPaymentProcessor
{
    private readonly IPaymentProcessor inner;

    public ValidationPaymentProcessorDecorator(IPaymentProcessor inner)
    {
        this.inner = inner;
    }

    public string Process(PaymentModel payment)
    {
        ArgumentNullException.ThrowIfNull(payment);
        if (payment.Amount <= 0 || payment.Amount > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(payment), "Amount must be 0.01 to 10,000.");
        }
        return inner.Process(payment); // only valid payments go further in
    }
}
