using System.Globalization;
using WorkflowEngine.Contexts;
using WorkflowEngine.Services;

namespace WorkflowEngine.Steps;

public class ChargePaymentStep : BaseWorkflowStep
{
    private readonly PaymentGatewayService gateway;

    public ChargePaymentStep(PaymentGatewayService gateway)
    {
        this.gateway = gateway;
    }

    public override string Name => "ChargePayment";

    public override int MaximumAttempts => 3; // payment gateways time out now and then: try up to 3 times

    public override Task ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken)
    {
        decimal amount = decimal.Parse(context.Get("Amount"), CultureInfo.InvariantCulture);
        context.Values["PaymentId"] = gateway.Charge(context.Get("Customer"), amount); // saved for later steps and for refund
        return Task.CompletedTask;
    }

    public override Task CompensateAsync(WorkflowContext context)
    {
        gateway.Refund(context.Get("PaymentId"));
        return Task.CompletedTask;
    }
}
