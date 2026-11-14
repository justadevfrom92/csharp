using WorkflowEngine.Contexts;
using WorkflowEngine.Models;
using WorkflowEngine.Outcomes;
using WorkflowEngine.Steps;

namespace WorkflowEngine.Engines;

// A small WORKFLOW ENGINE: runs a list of steps for one piece of business work (an order), and handles
// what every long-running process needs, so the steps don't have to:
//   - order:        steps run one after another
//   - conditions:   a step with ShouldRun = false is skipped
//   - retries:      a failing step is tried again, up to its MaximumAttempts
//   - compensation: if a step fails for good, the steps already done are UNDONE in reverse order (a "saga")
//   - resume:       steps already in context.CompletedSteps are skipped, so a crashed run can continue
//   - checkpoints:  after each step, "saveState" is called (e.g. write context.ToJson() to a database)
// Real engines (Elsa, Temporal, Azure Durable Functions, AWS Step Functions) add timers, waiting for people,
// versioning and a UI - but the core loop is this one.
public class WorkflowRunEngine
{
    private readonly Func<WorkflowContext, Task> saveState;

    public WorkflowRunEngine(Func<WorkflowContext, Task>? saveState = null)
    {
        this.saveState = saveState ?? (_ => Task.CompletedTask);
    }

    public async Task<WorkflowResultModel> RunAsync(IReadOnlyList<IWorkflowStep> steps, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(context);
        if (steps.Select(step => step.Name).Distinct().Count() != steps.Count)
        {
            throw new ArgumentException("Step names must be unique (they're used to remember progress).", nameof(steps));
        }

        foreach (IWorkflowStep step in steps)
        {
            if (context.CompletedSteps.Contains(step.Name))
            {
                context.History.Add($"{step.Name}: already done, skipped");
                continue;
            }
            if (!step.ShouldRun(context))
            {
                context.History.Add($"{step.Name}: not needed, skipped");
                continue;
            }

            Exception? lastError = await TryRunAsync(step, context, cancellationToken);
            if (lastError != null)
            {
                await CompensateAsync(steps, context);
                await saveState(context);
                return new WorkflowResultModel(WorkflowOutcome.RolledBack, step.Name, lastError.Message);
            }

            context.CompletedSteps.Add(step.Name);
            await saveState(context); // CHECKPOINT: if the process dies after this, the step won't run again
        }
        return new WorkflowResultModel(WorkflowOutcome.Completed, null, null);
    }

    // Returns null on success, or the last error after every attempt failed.
    private static async Task<Exception?> TryRunAsync(IWorkflowStep step, WorkflowContext context, CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= step.MaximumAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested(); // a shutdown is NOT a failure: no compensation, resume later
            try
            {
                await step.ExecuteAsync(context, cancellationToken);
                context.History.Add($"{step.Name}: done" + (attempt > 1 ? $" (attempt {attempt})" : ""));
                return null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                context.History.Add($"{step.Name}: attempt {attempt} failed - {exception.Message}");
                if (attempt == step.MaximumAttempts)
                {
                    return exception;
                }
            }
        }
        return null;
    }

    // Undo finished steps, newest first. One compensation failing must not stop the others.
    private static async Task CompensateAsync(IReadOnlyList<IWorkflowStep> steps, WorkflowContext context)
    {
        foreach (IWorkflowStep step in steps.Where(step => context.CompletedSteps.Contains(step.Name)).Reverse())
        {
            try
            {
                await step.CompensateAsync(context);
                context.History.Add($"{step.Name}: undone");
            }
            catch (Exception exception)
            {
                context.History.Add($"{step.Name}: UNDO FAILED - {exception.Message} (needs a person to fix)");
            }
        }
        context.CompletedSteps.Clear();
    }
}
