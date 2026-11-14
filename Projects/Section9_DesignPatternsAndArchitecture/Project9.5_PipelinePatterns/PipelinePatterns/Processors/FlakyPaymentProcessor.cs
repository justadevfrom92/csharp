using PipelinePatterns.Models;

namespace PipelinePatterns.Processors;

// The REAL work (pretend this calls a bank). It fails the first "failuresBeforeSuccess" times,
// like a real network service sometimes does - so the retry decorator has something to do.
public class FlakyPaymentProcessor : IPaymentProcessor
{
    private int failuresLeft;

    public FlakyPaymentProcessor(int failuresBeforeSuccess)
    {
        failuresLeft = failuresBeforeSuccess;
    }

    public int Calls { get; private set; }

    public string Process(PaymentModel payment)
    {
        Calls++;
        if (failuresLeft > 0)
        {
            failuresLeft--;
            throw new TimeoutException("Bank did not answer.");
        }
        return $"paid {payment.Amount:0.00} for {payment.OrderId}";
    }
}
