# Project 9.4 — Behavioral Patterns: Strategy, Command, State, Mediator, Observer

Five classic patterns in one small shop. Each one solves a different "how do these objects talk?" problem.

```bash
cd BehavioralPatterns
dotnet run
```

```bash
cd BehavioralPatterns.Tests
dotnet test
```

## The patterns

| Pattern | Problem it solves | Here | Folder |
|---|---|---|---|
| **Strategy** | Several ways to do one job, chosen at runtime, without a growing `if/else` | shipping price: standard / express / free over 100 | `Strategies/`, `Services/ShippingService.cs` |
| **Command** | Treat an action as an object, so it can be undone, redone, queued or logged | text editor edits with undo/redo | `Commands/`, `Services/CommandHistoryService.cs` |
| **State** | What's allowed depends on the current state; avoid `if (status == ...)` everywhere | order: pending → paid → shipped → delivered / cancelled | `States/`, `Contexts/OrderContext.cs` |
| **Mediator** | Callers shouldn't know (or depend on) who does the work | `mediator.Send(new PlaceOrderRequest(...))` finds `PlaceOrderHandler` | `Mediators/`, `Requests/`, `Handlers/`, `Results/` |
| **Observer** | Tell many interested parties "this happened" without knowing them | order placed → email + loyalty points | `Observers/`, `Publishers/`, `Events/` |

They work together: the **mediator** sends `PlaceOrderRequest` to its handler, the handler uses the **strategy**
to price shipping, then **publishes** `OrderPlacedEvent` to the **observers**.

### Strategy

```
ShippingService ──uses──► IShippingCostStrategy
                              ├─ StandardShippingStrategy
                              ├─ ExpressShippingStrategy
                              └─ FreeOverThresholdShippingStrategy
```

A new shipping option is **one new class** registered in DI. `ShippingService` never changes. All strategies
are injected as `IEnumerable<IShippingCostStrategy>` and looked up by name.
**Compare** 9.1, where a price rule is just a `Func<decimal, decimal>`: a lambda is enough when a strategy is one
small calculation; an interface is worth it when it has a name, several members, or its own dependencies.
7.3's `IEmailSender` (SMTP / file trap) is also a strategy.

### Command

Every edit is an object with `Execute()` **and** `Undo()`. `CommandHistoryService` keeps two stacks:
undo pushes onto redo, redo pushes back onto undo, and **a new edit clears redo** (the old future is gone).
`DeleteTextCommand` remembers the text it removed so `Undo` can put it back.

### State

```
pending --Pay--> paid --Ship--> shipped --Deliver--> delivered
   |              |
   +---Cancel-----+--> cancelled
```

Each state is a class. `BaseOrderState` makes every action throw ("Can't ship an order that is pending.");
each state overrides **only** the actions it allows and moves the order to the next state.
Adding a state (e.g. "returned") = one class + one changed state, instead of editing every `if`.

### Mediator

```csharp
PlaceOrderResult result = mediator.Send(new PlaceOrderRequest("Ann", 120m, "express", 3m));
```

The caller depends on **one** thing (`IMediator`), not on `ShippingService`, `OrderEventPublisher`, ... .
`ServiceProviderMediator` finds `IRequestHandler<PlaceOrderRequest, PlaceOrderResult>` in the DI container.
This is what the MediatR library does (also "CQRS" style: one request = one handler). **Cost:** you can't
"go to definition" from the call to the handler; for a small app, calling a service directly is simpler.

### Observer

`OrderEventPublisher.Subscribe(observer)` returns an `IDisposable` — dispose it to unsubscribe (forgetting to
unsubscribe is the classic memory leak). `Publish` tells every observer, and **one failing observer doesn't stop
the others**: failures are collected and thrown together as an `AggregateException` at the end.
Other observer styles in this repo: C# `event`s (5.1), domain events (4.2), SignalR (8.8), `IObservable` (9.8).

## Code snippets

**Strategy: one interface, several classes, picked by name**, from `Strategies/` and `Services/ShippingService.cs`

```csharp
public interface IShippingCostStrategy
{
    string Name { get; }

    decimal Calculate(decimal orderTotal, decimal weightKilograms);
}

public ShippingService(IEnumerable<IShippingCostStrategy> strategies)
{
    this.strategies = strategies.ToDictionary(strategy => strategy.Name, StringComparer.OrdinalIgnoreCase);
}

serviceCollection.AddSingleton<IShippingCostStrategy, StandardShippingStrategy>();
serviceCollection.AddSingleton<IShippingCostStrategy, ExpressShippingStrategy>();
```

**Command: an action with Execute and Undo**, from `Commands/InsertTextCommand.cs` and `Services/CommandHistoryService.cs`

```csharp
public void Execute()
{
    document.Text.Insert(position, text);
}

public void Undo()
{
    document.Text.Remove(position, text.Length);
}

public void Execute(IEditorCommand command)
{
    command.Execute();
    undoStack.Push(command);
    redoStack.Clear();
}

public bool Undo()
{
    if (!undoStack.TryPop(out IEditorCommand? command))
    {
        return false;
    }
    command.Undo();
    redoStack.Push(command);
    return true;
}
```

**State: each state allows only its own actions**, from `States/`

```csharp
public abstract class BaseOrderState : IOrderState
{
    public virtual void Ship(OrderContext order)
    {
        throw new InvalidOperationException($"Can't ship an order that is {Name}.");
    }
}

public class PaidState : BaseOrderState
{
    public override string Name => "paid";

    public override void Ship(OrderContext order)
    {
        order.MoveTo(new ShippedState());
    }
}
```

**Mediator: find the handler for a request**, from `Mediators/ServiceProviderMediator.cs`

```csharp
public TResult Send<TResult>(IRequest<TResult> request)
{
    Type handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResult));
    object handler = services.GetService(handlerType)
        ?? throw new InvalidOperationException($"No handler registered for {request.GetType().Name}.");
    return ((dynamic)handler).Handle((dynamic)request);
}

public record PlaceOrderRequest(string Customer, decimal Total, string ShippingOption, decimal WeightKilograms) : IRequest<PlaceOrderResult>;
```

**Observer: subscribe with an `IDisposable`, publish to everyone**, from `Publishers/OrderEventPublisher.cs`

```csharp
using IDisposable subscription = publisher.Subscribe(new EmailOrderPlacedObserver());

foreach (IOrderPlacedObserver observer in observers.ToList())
{
    try
    {
        observer.OnOrderPlaced(placedEvent);
    }
    catch (Exception exception)
    {
        failures.Add(exception);
    }
}
```

## Why these types

| Choice | Why | Why not the alternative |
|---|---|---|
| `interface` for strategies, commands, states, observers | Several interchangeable implementations — the textbook reason for an interface. | — |
| `abstract class BaseOrderState` | Real shared code (the "not allowed" default for every action). | An interface alone would repeat the same `throw` in every state. |
| `Dictionary<string, IShippingCostStrategy>` with `OrdinalIgnoreCase` | Lookup by the name the customer picked; "Express" = "express". | A `switch` must change for every new option. |
| Two `Stack<IEditorCommand>` | Undo/redo is last-in-first-out. | — |
| `record` requests, results, events | Plain data that never changes. | — |
| `IReadOnlyList<string> History` | Callers can read the state history, not rewrite it. | — |
| `IDisposable` from `Subscribe` | `using` unsubscribes automatically. | A separate `Unsubscribe(observer)` is easy to forget. |
| `AggregateException` | Several observers can fail; report all of them. | Stopping at the first failure would skip the others. |
| `dynamic` in the mediator | The handler type is only known at runtime. | Reflection `Invoke` wraps every error in `TargetInvocationException`. |

## Tests

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| Strategy | each option prices correctly (case ignored, threshold edge) | unknown option → error listing the valid ones | empty/null option, negative total, zero weight, null list |
| Command | 3 edits, undo, undo, redo | undo/redo with nothing → `false`; a new edit clears redo | bad position, null command, empty text → throws, nothing recorded |
| State | happy path + cancel after paying | ship before paying; cancel after shipping → throws, state unchanged | cancelled order accepts nothing; `MoveTo(null)` |
| Mediator | request → handler → observers | bad option / zero total → `Succeeded = false`, nobody told | `null` request; request with no handler |
| Observer | all told until unsubscribed | one fails → others still told, then `AggregateException`; double subscribe = once | `null` event / observer |
