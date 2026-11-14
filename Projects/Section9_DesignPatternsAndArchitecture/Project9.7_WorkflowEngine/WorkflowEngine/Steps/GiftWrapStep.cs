using WorkflowEngine.Contexts;

namespace WorkflowEngine.Steps;

// A CONDITIONAL step: only runs when the customer asked for gift wrapping.
public class GiftWrapStep : BaseWorkflowStep
{
    public override string Name => "GiftWrap";

    public override bool ShouldRun(WorkflowContext context)
    {
        return context.Values.GetValueOrDefault("GiftWrap") == "yes";
    }

    public override Task ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken)
    {
        context.Values["Wrapping"] = "red paper";
        return Task.CompletedTask;
    }
}
