using PipelinePatterns.Models;

namespace PipelinePatterns.Variations;

// The decorator pipeline as FUNCTIONS, the way ASP.NET Core builds its middleware pipeline:
// each step is "given the next step, return a new step that wraps it". No classes per layer (Decorators/, original).
public static class PaymentMiddlewareVariation
{
    // A step: takes the payment and the NEXT step, returns the result.
    public delegate string PaymentStep(PaymentModel payment, Func<PaymentModel, string> next);

    // Builds the pipeline from the LAST step backwards, so the first step in the list runs first.
    public static Func<PaymentModel, string> Build(IReadOnlyList<PaymentStep> steps, Func<PaymentModel, string> endpoint)
    {
        Func<PaymentModel, string> pipeline = endpoint;
        for (int index = steps.Count - 1; index >= 0; index--)
        {
            PaymentStep step = steps[index];
            Func<PaymentModel, string> next = pipeline;
            pipeline = payment => step(payment, next);
        }
        return pipeline;
    }

    public static PaymentStep Logging(List<string> log)
    {
        return (payment, next) =>
        {
            log.Add($"start {payment.OrderId}");
            string result = next(payment);
            log.Add($"ok {result}");
            return result;
        };
    }

    public static PaymentStep Validation()
    {
        return (payment, next) =>
        {
            if (payment.Amount <= 0 || payment.Amount > 10_000)
            {
                throw new ArgumentOutOfRangeException(nameof(payment), "Amount must be 0.01 to 10,000.");
            }
            return next(payment);
        };
    }
}
