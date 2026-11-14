using BehavioralPatterns.Commands;
using BehavioralPatterns.Contexts;
using BehavioralPatterns.Handlers;
using BehavioralPatterns.Mediators;
using BehavioralPatterns.Models;
using BehavioralPatterns.Observers;
using BehavioralPatterns.Publishers;
using BehavioralPatterns.Requests;
using BehavioralPatterns.Results;
using BehavioralPatterns.Services;
using BehavioralPatterns.Strategies;
using Microsoft.Extensions.DependencyInjection;

// ---------- Wiring (dependency injection) ----------
var serviceCollection = new ServiceCollection();
serviceCollection.AddSingleton<IShippingCostStrategy, StandardShippingStrategy>();   // several implementations
serviceCollection.AddSingleton<IShippingCostStrategy, ExpressShippingStrategy>();    // of ONE interface:
serviceCollection.AddSingleton<IShippingCostStrategy, FreeOverThresholdShippingStrategy>(); // all injected as IEnumerable
serviceCollection.AddSingleton<ShippingService>();
serviceCollection.AddSingleton<OrderEventPublisher>();
serviceCollection.AddTransient<IRequestHandler<PlaceOrderRequest, PlaceOrderResult>, PlaceOrderHandler>();
serviceCollection.AddSingleton<IMediator, ServiceProviderMediator>();
using ServiceProvider services = serviceCollection.BuildServiceProvider();

// ---------- 1. Strategy ----------
var shipping = services.GetRequiredService<ShippingService>();
Console.WriteLine("1. STRATEGY - shipping for a 60.00 order weighing 2 kg:");
foreach (string option in shipping.OptionNames)
{
    Console.WriteLine($"   {option,-14} {shipping.Quote(option, 60m, 2m):0.00}");
}

// ---------- 2. Observer + 3. Mediator ----------
var publisher = services.GetRequiredService<OrderEventPublisher>();
var email = new EmailOrderPlacedObserver();
var loyalty = new LoyaltyPointsOrderPlacedObserver();
using IDisposable emailSubscription = publisher.Subscribe(email);
IDisposable loyaltySubscription = publisher.Subscribe(loyalty);

var mediator = services.GetRequiredService<IMediator>();
PlaceOrderResult first = mediator.Send(new PlaceOrderRequest("Ann", 120m, "free-over-100", 3m));
loyaltySubscription.Dispose(); // loyalty stops listening
PlaceOrderResult second = mediator.Send(new PlaceOrderRequest("Ann", 40m, "express", 1m));
PlaceOrderResult failed = mediator.Send(new PlaceOrderRequest("Ann", 40m, "teleport", 1m));

Console.WriteLine("2/3. MEDIATOR sends requests to their handler; the handler PUBLISHES to OBSERVERS:");
Console.WriteLine($"   order {first.OrderId}: shipping {first.Shipping:0.00}, total {first.GrandTotal:0.00}");
Console.WriteLine($"   order {second.OrderId}: shipping {second.Shipping:0.00}, total {second.GrandTotal:0.00}");
Console.WriteLine($"   failed: {failed.Error}");
Console.WriteLine($"   emails sent: {email.SentEmails.Count} (both orders)");
Console.WriteLine($"   Ann's loyalty points: {loyalty.PointsByCustomer["Ann"]} (only the first order - it unsubscribed)");

// ---------- 4. Command ----------
var document = new TextDocumentModel();
var history = new CommandHistoryService();
history.Execute(new InsertTextCommand(document, 0, "Hello"));
history.Execute(new InsertTextCommand(document, 5, " world"));
history.Execute(new DeleteTextCommand(document, 0, 6));
Console.WriteLine($"4. COMMAND - after 3 edits: '{document}'");
history.Undo();
Console.WriteLine($"   undo:  '{document}'");
history.Undo();
Console.WriteLine($"   undo:  '{document}'");
history.Redo();
Console.WriteLine($"   redo:  '{document}'");

// ---------- 5. State ----------
var order = new OrderContext();
order.Pay();
order.Ship();
try
{
    order.Cancel();
}
catch (InvalidOperationException exception)
{
    Console.WriteLine($"5. STATE - {exception.Message}");
}
order.Deliver();
Console.WriteLine($"   history: {string.Join(" -> ", order.History)}");
