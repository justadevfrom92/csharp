using PipelinePatterns.Decorators;
using PipelinePatterns.Filters;
using PipelinePatterns.Handlers;
using PipelinePatterns.Interceptors;
using PipelinePatterns.Models;
using PipelinePatterns.Pipelines;
using PipelinePatterns.Processors;
using PipelinePatterns.Services;

// ---------- 1. Pipes and filters ----------
TextPipeline cleanLogLine = new TextPipeline()
    .Add(new TrimWhitespaceFilter())
    .Add(new MaskEmailFilter())
    .Add(new MaskCardNumberFilter())
    .Add(new MaximumLengthFilter(60));
Console.WriteLine("1. PIPES AND FILTERS");
Console.WriteLine("   " + cleanLogLine.Run("  Payment   by ann@example.com   with card 4111 1111 1111 1234   failed  "));

// ---------- 2. Chain of responsibility ----------
var approvals = new TeamLeadApprovalHandler();
approvals.SetNext(new ManagerApprovalHandler()).SetNext(new DirectorApprovalHandler());
Console.WriteLine("2. CHAIN OF RESPONSIBILITY");
foreach (decimal amount in new[] { 250m, 4_000m, 75_000m, 500_000m })
{
    Console.WriteLine($"   {amount,10:N2} -> {approvals.Approve(new ExpenseModel("Ann", amount, "Laptop"))}");
}

// ---------- 3. Decorator pipeline ----------
var log = new List<string>();
IPaymentProcessor payments =
    new LoggingPaymentProcessorDecorator(            // outermost: sees everything
        new ValidationPaymentProcessorDecorator(     // rejects bad payments before any retry
            new RetryPaymentProcessorDecorator(      // retries only the real call
                new FlakyPaymentProcessor(failuresBeforeSuccess: 2), maximumAttempts: 3)),
        log);
Console.WriteLine("3. DECORATOR PIPELINE");
Console.WriteLine("   " + payments.Process(new PaymentModel("A-1", 49.99m)));
try
{
    payments.Process(new PaymentModel("A-2", -5m));
}
catch (ArgumentOutOfRangeException)
{
}
Console.WriteLine($"   log: {string.Join(" | ", log)}");

// ---------- 4. Interceptor / AOP ----------
var callLog = new List<string>();
IStockService stock = LoggingInterceptor<IStockService>.Wrap(new StockService(), callLog);
stock.Reserve("KB-01", 3);
stock.GetStock("KB-01");
try
{
    stock.Reserve("MS-01", 5);
}
catch (InvalidOperationException)
{
}
Console.WriteLine("4. INTERCEPTOR / AOP (no logging code inside StockService)");
foreach (string line in callLog)
{
    Console.WriteLine("   " + line);
}
