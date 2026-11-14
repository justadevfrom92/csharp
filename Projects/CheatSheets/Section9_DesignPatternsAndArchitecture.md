# Cheat Sheet — Section 9: Design Patterns & Architecture

Projects: [9.1](../Section9_DesignPatternsAndArchitecture/Project9.1_OperatorsAndLambdas), [9.2](../Section9_DesignPatternsAndArchitecture/Project9.2_JsonYamlConfiguration), [9.3](../Section9_DesignPatternsAndArchitecture/Project9.3_FluentBuilderAndDsl), [9.4](../Section9_DesignPatternsAndArchitecture/Project9.4_BehavioralPatterns), [9.5](../Section9_DesignPatternsAndArchitecture/Project9.5_PipelinePatterns), [9.6](../Section9_DesignPatternsAndArchitecture/Project9.6_Dataflow), [9.7](../Section9_DesignPatternsAndArchitecture/Project9.7_WorkflowEngine), [9.8](../Section9_DesignPatternsAndArchitecture/Project9.8_ReactiveExtensions).
Where each pattern appears in the whole repo: [DESIGN_PATTERNS.md](../DESIGN_PATTERNS.md).

## Operator overloading

```csharp
public record Money(decimal Amount, string Currency)
{
    public static Money operator +(Money left, Money right)
    {
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator *(Money money, decimal factor)
    {
        return new Money(money.Amount * factor, money.Currency);
    }

    public static bool operator <(Money left, Money right)   // comparison operators come in pairs
    {
        return left.Amount < right.Amount;
    }

    public static bool operator >(Money left, Money right)
    {
        return left.Amount > right.Amount;
    }

    public static explicit operator decimal(Money money)      // (decimal)money
    {
        return money.Amount;
    }
}
```

## Lambdas

```csharp
Func<int, int, int> add = (a, b) => a + b;
Action<string> print = text => Console.WriteLine(text);
var multiply = (int a, int b) => a * b;                   // natural type
Func<string, int> length = static text => text.Length;     // can't capture
Expression<Func<Product, bool>> condition = product => product.Price > 20;          // as data (EF uses this)
Func<Product, bool> compiled = condition.Compile();

Func<decimal, decimal> MakeMultiplier(decimal factor)        // closure: the lambda remembers "factor"
{
    return price => price * factor;
}
```

## JSON / YAML configuration

```csharp
IConfigurationRoot configuration = new ConfigurationBuilder()
    .SetBasePath(folder)
    .AddJsonFile("shop.json", optional: false)
    .AddYamlFile($"shop.{environment}.yaml", optional: true)   // package NetEscapades.Configuration.Yaml
    .AddEnvironmentVariables(prefix: "SHOP_")
    .Build();
ShopSettings settings = configuration.GetSection("Shop").Get<ShopSettings>()!;
string? first = configuration["Shop:Shipping:Countries:0"];
```

Later sources win; objects merge key by key; **lists merge by position** (a later file can't shorten a list).

## Fluent builder

```csharp
public class OrderBuilder
{
    public OrderBuilder ForCustomer(string name)
    {
        customer = name;
        return this;               // the whole trick
    }

    public OrderModel Build()
    {
        if (customer == null)
        {
            throw new InvalidOperationException("Customer is required.");
        }
        return new OrderModel(customer, lines.ToList());
    }
}

OrderModel order = new OrderBuilder().ForCustomer("Ann").AddLine("KB-01", 2, 25m).Build();
```

## DSL (external)

```csharp
// "WHEN total > 100 THEN discount 10%"
var pattern = new Regex(@"^WHEN\s+(?<field>\w+)\s*(?<operator>>=|<=|>|<|=)\s*(?<value>\S+)\s+THEN\s+(?<action>\w+)\s+(?<amount>[0-9.]+)(?<percent>%?)$", RegexOptions.IgnoreCase);
Match match = pattern.Match(line);
if (!match.Success)
{
    throw new FormatException($"Line {lineNumber}: ...");   // always say which line
}
```

## Strategy

```csharp
public interface IShippingCostStrategy
{
    string Name { get; }

    decimal Calculate(decimal total, decimal weight);
}
services.AddSingleton<IShippingCostStrategy, StandardShippingStrategy>();
services.AddSingleton<IShippingCostStrategy, ExpressShippingStrategy>();
public ShippingService(IEnumerable<IShippingCostStrategy> strategies)   // all of them injected
```

## Command (undo / redo)

```csharp
public interface IEditorCommand
{
    void Execute();

    void Undo();
}

command.Execute();
undoStack.Push(command);
redoStack.Clear();
```

## State

```csharp
public abstract class BaseOrderState
{
    public virtual void Ship(OrderContext order)
    {
        throw new InvalidOperationException($"Can't ship an order that is {Name}.");
    }
}

public class PaidState : BaseOrderState
{
    public override void Ship(OrderContext order)
    {
        order.MoveTo(new ShippedState());
    }
}
```

## Mediator

```csharp
public interface IRequest<TResult>
{
}

public interface IRequestHandler<TRequest, TResult> where TRequest : IRequest<TResult>
{
    TResult Handle(TRequest request);
}

PlaceOrderResult result = mediator.Send(new PlaceOrderRequest("Ann", 120m, "express", 3m));
```

## Observer

```csharp
IDisposable subscription = publisher.Subscribe(new EmailOrderPlacedObserver()); // Dispose() = unsubscribe
foreach (IOrderPlacedObserver observer in observers.ToList())
{
    observer.OnOrderPlaced(placedEvent);
}
```

## Pipes and filters / Chain of responsibility / Decorator

```csharp
string clean = new TextPipeline().Add(new TrimWhitespaceFilter()).Add(new MaskEmailFilter()).Run(text); // every filter runs

teamLead.SetNext(manager).SetNext(director);   // the first one that can, handles it
string answer = teamLead.Approve(expense);

IPaymentProcessor payments = new LoggingDecorator(new ValidationDecorator(new RetryDecorator(bank, 3)), log); // layers
```

## Interceptor / AOP (`DispatchProxy`)

```csharp
public class LoggingInterceptor<T> : DispatchProxy where T : class
{
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        log.Add($"{targetMethod!.Name}({string.Join(", ", args ?? [])})");
        return targetMethod.Invoke(target, args);
    }
}
IStockService stock = LoggingInterceptor<IStockService>.Wrap(new StockService(), log);
```

## Dataflow (built into .NET)

```csharp
var parse = new TransformBlock<string, Order>(line => Parse(line), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 4, BoundedCapacity = 16 });
var batch = new BatchBlock<Order>(100);
var save = new ActionBlock<Order[]>(orders => SaveAsync(orders));
var propagate = new DataflowLinkOptions { PropagateCompletion = true };
parse.LinkTo(batch, propagate, order => order.IsValid);
batch.LinkTo(save, propagate);

await parse.SendAsync(line);   // waits when full (backpressure)
parse.Complete();
await save.Completion;
```

## Workflow / saga

```csharp
foreach (IWorkflowStep step in steps)
{
    if (context.CompletedSteps.Contains(step.Name))
    {
        continue;                                  // resume
    }
    try
    {
        await step.ExecuteAsync(context, cancellationToken);
        context.CompletedSteps.Add(step.Name);
        await saveState(context);                  // checkpoint
    }
    catch (Exception)
    {
        foreach (IWorkflowStep done in steps.Where(s => context.CompletedSteps.Contains(s.Name)).Reverse())
        {
            await done.CompensateAsync(context);   // undo, newest first
        }
        throw;
    }
}
```

## Rx (`System.Reactive`)

```csharp
using var ticks = new Subject<PriceTick>();
using IDisposable subscription = ticks
    .Where(tick => tick.Symbol == "MSFT")
    .Buffer(3, 1)                                    // sliding window of 3
    .Select(window => window.Average(tick => tick.Price))
    .Subscribe(average => Console.WriteLine(average));
ticks.OnNext(new PriceTick("MSFT", 400m));

ticks.Throttle(TimeSpan.FromMilliseconds(100), scheduler);    // only after 100 ms of quiet
ticks.Buffer(TimeSpan.FromSeconds(1), scheduler).Select(batch => batch.Count); // per second

var scheduler = new TestScheduler();                          // tests: fake clock (Microsoft.Reactive.Testing)
scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);
```
