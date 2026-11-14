namespace WorkflowEngine.Services;

// Fake payment gateway. "FailuresBeforeSuccess" makes it time out a few times (to show retries);
// "IsDown" makes every call fail (to show compensation).
public class PaymentGatewayService
{
    public int FailuresBeforeSuccess { get; set; }

    public bool IsDown { get; set; }

    public List<string> Charges { get; } = new();

    public List<string> Refunds { get; } = new();

    public int Calls { get; private set; }

    public string Charge(string customer, decimal amount)
    {
        Calls++;
        if (IsDown)
        {
            throw new InvalidOperationException("Payment gateway is down.");
        }
        if (FailuresBeforeSuccess > 0)
        {
            FailuresBeforeSuccess--;
            throw new TimeoutException("Payment gateway timed out.");
        }
        string paymentId = $"PAY-{Charges.Count + 1}";
        Charges.Add($"{paymentId} {customer} {amount:0.00}");
        return paymentId;
    }

    public void Refund(string paymentId)
    {
        Refunds.Add(paymentId);
    }
}
