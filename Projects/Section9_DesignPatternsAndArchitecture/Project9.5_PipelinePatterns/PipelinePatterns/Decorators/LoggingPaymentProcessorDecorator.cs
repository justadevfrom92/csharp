using PipelinePatterns.Models;
using PipelinePatterns.Processors;

namespace PipelinePatterns.Decorators;

// Writes what went in and what came out (or failed).
public class LoggingPaymentProcessorDecorator : IPaymentProcessor
{
    private readonly IPaymentProcessor inner;
    private readonly List<string> log;

    public LoggingPaymentProcessorDecorator(IPaymentProcessor inner, List<string> log)
    {
        this.inner = inner;
        this.log = log;
    }

    public string Process(PaymentModel payment)
    {
        log.Add($"start {payment?.OrderId}");
        try
        {
            string result = inner.Process(payment!);
            log.Add($"ok {result}");
            return result;
        }
        catch (Exception exception)
        {
            log.Add($"failed {exception.GetType().Name}");
            throw;
        }
    }
}
