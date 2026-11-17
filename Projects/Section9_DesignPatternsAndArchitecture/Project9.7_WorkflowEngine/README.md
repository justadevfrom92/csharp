# Project 9.7 — Workflow Engine

A small engine that runs a **business process made of steps** — reserve stock → charge payment → gift wrap (if
asked) → create shipment → send confirmation — and handles everything such processes need:
**retries, conditional steps, compensation (undo) when a step fails, checkpoints, and resuming after a crash.**

```bash
cd WorkflowEngine
dotnet run
```

```bash
cd WorkflowEngine.Tests
dotnet test
```

## Why a workflow engine

An order touches several systems (warehouse, payment gateway, courier, email). You **can't** wrap them in one
database transaction, any of them can fail or time out, and the process itself can crash half-way. Without an
engine, every process re-implements "retry this, undo that, don't charge twice". With one, each **step** only
knows how to do and undo its own piece; the **engine** handles the rest.

| Feature | How | Where |
|---|---|---|
| Steps in order | a list of `IWorkflowStep` | `Engines/WorkflowRunEngine.cs` |
| Conditional steps | `ShouldRun(context)` → `false` = skipped | `Steps/GiftWrapStep.cs` |
| Retries | `MaximumAttempts` per step (payment: 3) | `Steps/ChargePaymentStep.cs` |
| **Compensation** ("saga") | a step fails for good → `CompensateAsync` on every finished step, **newest first** (refund, then release stock) | `CompensateAsync` in each step |
| Checkpoints | after each step the engine calls `saveState(context)` | `WorkflowRunEngine` constructor |
| **Resume** | steps in `context.CompletedSteps` are skipped, so a restarted run continues where it stopped and **doesn't charge twice** | `WorkflowContext.ToJson()` / `FromJson()` |
| Crash ≠ failure | `OperationCanceledException` (shutdown) stops the run **without** compensating, so it can resume | `TryRunAsync` |

```
ReserveStock ─► ChargePayment ─► GiftWrap? ─► CreateShipment ─► SendConfirmation
     ▲                ▲                              │ fails for good
     └── release ◄────┴── refund ◄───────────────────┘   (compensation, in reverse)
```

**Saga** is the name for "a long process made of steps, where each step has an undo". It replaces a transaction
when the work spans several services.

**Real engines** — Elsa (.NET), Temporal, Azure Durable Functions, AWS Step Functions — add timers ("wait 3
days"), waiting for a person to approve, versioning and dashboards. The core loop is the one in this project.

## Code snippets

**A step: do, and undo**, from `Steps/ChargePaymentStep.cs`

```csharp
public class ChargePaymentStep : BaseWorkflowStep
{
    public override string Name => "ChargePayment";

    public override int MaximumAttempts => 3;

    public override Task ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken)
    {
        decimal amount = decimal.Parse(context.Get("Amount"), CultureInfo.InvariantCulture);
        context.Values["PaymentId"] = gateway.Charge(context.Get("Customer"), amount);
        return Task.CompletedTask;
    }

    public override Task CompensateAsync(WorkflowContext context)
    {
        gateway.Refund(context.Get("PaymentId"));
        return Task.CompletedTask;
    }
}
```

**A conditional step**, from `Steps/GiftWrapStep.cs`

```csharp
public override bool ShouldRun(WorkflowContext context)
{
    return context.Values.GetValueOrDefault("GiftWrap") == "yes";
}
```

**The engine loop: skip, run with retries, compensate, checkpoint**, from `Engines/WorkflowRunEngine.cs`

```csharp
foreach (IWorkflowStep step in steps)
{
    if (context.CompletedSteps.Contains(step.Name))
    {
        continue;                                   // resume: already done
    }
    if (!step.ShouldRun(context))
    {
        continue;                                   // condition not met
    }
    Exception? lastError = await TryRunAsync(step, context, cancellationToken);
    if (lastError != null)
    {
        await CompensateAsync(steps, context);      // undo finished steps, newest first
        return new WorkflowResultModel(WorkflowOutcome.RolledBack, step.Name, lastError.Message);
    }
    context.CompletedSteps.Add(step.Name);
    await saveState(context);                       // checkpoint
}
```

**Undo in reverse order**

```csharp
foreach (IWorkflowStep step in steps.Where(step => context.CompletedSteps.Contains(step.Name)).Reverse())
{
    await step.CompensateAsync(context);
}
```

**Save and resume**, from `Contexts/WorkflowContext.cs` and `Program.cs`

```csharp
string json = context.ToJson();                         // after every step (database, file, ...)
WorkflowContext resumed = WorkflowContext.FromJson(json);
await new WorkflowRunEngine().RunAsync(fulfilOrder, resumed);
```

## Variations

The same process hand-written without the engine, in `WorkflowEngine/Variations/FulfilOrderVariation.cs`,
compared by `WorkflowEngine.Tests/VariationsTest.cs`.

| Way | Shipping fails | Crash after payment, then restart | Retries / conditional steps | The difference |
|---|---|---|---|---|
| `WorkflowRunEngine` + step classes (original) | refund + release, newest first | resumes from saved JSON: charged **once** | ✅ built in | one engine for every process |
| one method with `try/catch` per step (`WithTryCatch`) | refund + release — **same result** | starts again from the top: charged **twice** | write them yourself | readable for one short process; every new process repeats the plumbing |

**Other ways to get the same features** (documented):

| Need | Original | Alternative |
|---|---|---|
| undo across steps | compensation (saga) | a database transaction (6.2) — only when every step uses the **same** database |
| retries | `MaximumAttempts` loop | Polly / `Microsoft.Extensions.Resilience` pipeline around a step (8.3) |
| allowed state changes | step order in a list | a state machine (9.4 State, or the Stateless library) |
| running for days, timers, waiting for people | — | Elsa, Temporal, Azure Durable Functions, AWS Step Functions |
| saving progress | `context.ToJson()` after each step | a database table of completed steps, written in the **same transaction** as each step's own change, so they can never disagree |

## Why these types

| Choice | Why | Why not the alternative |
|---|---|---|
| `IWorkflowStep` + `abstract class BaseWorkflowStep` | The engine needs one shape for every step; the base class gives defaults (1 attempt, always run, nothing to undo). | Every step repeating the defaults. |
| `WorkflowContext` with `Dictionary<string, string> Values` | Steps share data (the payment id) and the whole thing saves as plain JSON. | Typed properties per workflow would tie the engine to one process. |
| `List<string> CompletedSteps` (names) | Easy to save and read; names identify steps across restarts. | Positions break when a step is added. |
| `enum WorkflowOutcome` | Two named results. | A `bool` can't say "rolled back". |
| `Func<WorkflowContext, Task> saveState` | The engine doesn't care where state is stored; tests use it to simulate a crash. | Hard-coding a file or database. |
| `Exception?` returned from `TryRunAsync` | "Failed after all attempts" is a normal result here, not an exceptional one. | Throwing and catching again one level up. |

## Tests

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| Run | all steps run, payment retried, gift wrap done; gift wrap skipped when not asked | shipment fails → refund + stock released, in reverse; payment down → 3 tries then roll back; not enough stock → roll back at step 1 | missing value → rolled back with a clear error; `null` steps / context; duplicate step names |
| Resume | crash after payment → resumed from JSON, charged once | cancellation stops without undoing anything | empty, blank, broken or `null` JSON |
