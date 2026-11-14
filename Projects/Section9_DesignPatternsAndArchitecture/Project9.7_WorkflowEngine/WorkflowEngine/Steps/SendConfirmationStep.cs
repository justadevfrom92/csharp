using WorkflowEngine.Contexts;

namespace WorkflowEngine.Steps;

public class SendConfirmationStep : BaseWorkflowStep
{
    public List<string> SentEmails { get; } = new();

    public override string Name => "SendConfirmation";

    public override Task ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken)
    {
        SentEmails.Add($"To {context.Get("Customer")}: paid with {context.Get("PaymentId")}, on its way.");
        return Task.CompletedTask;
    }
}
