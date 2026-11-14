using WorkflowEngine.Contexts;
using WorkflowEngine.Engines;
using WorkflowEngine.Models;
using WorkflowEngine.Services;
using WorkflowEngine.Steps;

var warehouse = new WarehouseService();
var gateway = new PaymentGatewayService();
var confirmation = new SendConfirmationStep();
List<IWorkflowStep> fulfilOrder =
[
    new ReserveStockStep(warehouse),
    new ChargePaymentStep(gateway),
    new GiftWrapStep(),
    new CreateShipmentStep(warehouse),
    confirmation,
];

WorkflowContext NewOrder(string customer, string sku, int quantity, string country, bool giftWrap)
{
    var context = new WorkflowContext();
    context.Values["Customer"] = customer;
    context.Values["Sku"] = sku;
    context.Values["Quantity"] = quantity.ToString();
    context.Values["Amount"] = (quantity * 25m).ToString(System.Globalization.CultureInfo.InvariantCulture);
    context.Values["Country"] = country;
    context.Values["GiftWrap"] = giftWrap ? "yes" : "no";
    return context;
}

void Print(string title, WorkflowResultModel result, WorkflowContext context)
{
    Console.WriteLine($"--- {title}: {result.Outcome}" + (result.FailedStep == null ? "" : $" at {result.FailedStep}"));
    foreach (string line in context.History)
    {
        Console.WriteLine($"    {line}");
    }
}

var engine = new WorkflowRunEngine();

// 1. Happy path, with gift wrap, and a payment that times out twice before working (retries).
gateway.FailuresBeforeSuccess = 2;
WorkflowContext first = NewOrder("Ann", "KB-01", 2, "US", giftWrap: true);
Print("1. retries + conditional step", await engine.RunAsync(fulfilOrder, first), first);

// 2. Shipping fails for good -> payment refunded, stock released (compensation).
WorkflowContext second = NewOrder("Bob", "KB-01", 3, "XX", giftWrap: false);
Print("2. compensation", await engine.RunAsync(fulfilOrder, second), second);
Console.WriteLine($"    stock KB-01 back to {warehouse.Stock["KB-01"]}, refunds: {string.Join(", ", gateway.Refunds)}");

// 3. The process "crashes" after a checkpoint; a NEW engine resumes from the saved JSON.
string? savedJson = null;
var crashing = new WorkflowRunEngine(context =>
{
    savedJson = context.ToJson();
    if (context.CompletedSteps.Contains("ChargePayment"))
    {
        throw new OperationCanceledException("power cut!"); // simulated crash right after payment
    }
    return Task.CompletedTask;
});
WorkflowContext third = NewOrder("Cid", "MS-01", 1, "US", giftWrap: false);
try
{
    await crashing.RunAsync(fulfilOrder, third);
}
catch (OperationCanceledException exception)
{
    Console.WriteLine($"--- 3. crashed: {exception.Message} (saved state: {third.CompletedSteps.Count} steps done)");
}
WorkflowContext resumed = WorkflowContext.FromJson(savedJson!);
Print("3. resumed from saved JSON", await new WorkflowRunEngine().RunAsync(fulfilOrder, resumed), resumed);
Console.WriteLine($"    payments taken in total: {gateway.Charges.Count} (Cid was charged ONCE, not twice)");
