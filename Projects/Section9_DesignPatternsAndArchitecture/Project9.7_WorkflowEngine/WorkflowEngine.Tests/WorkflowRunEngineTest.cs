using WorkflowEngine.Contexts;
using WorkflowEngine.Engines;
using WorkflowEngine.Models;
using WorkflowEngine.Outcomes;
using WorkflowEngine.Services;
using WorkflowEngine.Steps;

namespace WorkflowEngine.Tests;

// PositivePositive = valid input -> works; PositiveNegative = well-formed input that breaks a rule -> rolled back;
// NegativeNegative = broken or missing input -> rejected.
public class WorkflowRunEngineTest
{
    private readonly WarehouseService warehouse = new WarehouseService();
    private readonly PaymentGatewayService gateway = new PaymentGatewayService();
    private readonly SendConfirmationStep confirmation = new SendConfirmationStep();
    private readonly List<IWorkflowStep> steps;

    public WorkflowRunEngineTest()
    {
        steps = [new ReserveStockStep(warehouse), new ChargePaymentStep(gateway), new GiftWrapStep(), new CreateShipmentStep(warehouse), confirmation];
    }

    private static WorkflowContext Order(string sku = "KB-01", int quantity = 2, string country = "US", bool giftWrap = false)
    {
        var context = new WorkflowContext();
        context.Values["Customer"] = "Ann";
        context.Values["Sku"] = sku;
        context.Values["Quantity"] = quantity.ToString();
        context.Values["Amount"] = "50.00";
        context.Values["Country"] = country;
        context.Values["GiftWrap"] = giftWrap ? "yes" : "no";
        return context;
    }

    // ---------- run, retry, conditional ----------

    [Fact]
    public async Task Run_PositivePositive_AllStepsRun_WithRetriesAndConditionalStep()
    {
        gateway.FailuresBeforeSuccess = 2;
        WorkflowContext context = Order(giftWrap: true);

        WorkflowResultModel result = await new WorkflowRunEngine().RunAsync(steps, context);

        Assert.Equal(WorkflowOutcome.Completed, result.Outcome);
        Assert.Equal(["ReserveStock", "ChargePayment", "GiftWrap", "CreateShipment", "SendConfirmation"], context.CompletedSteps);
        Assert.Equal(3, gateway.Calls);              // 2 timeouts + 1 success, within MaximumAttempts = 3
        Assert.Equal("red paper", context.Values["Wrapping"]);
        Assert.Equal(8, warehouse.Stock["KB-01"]);
        Assert.Single(confirmation.SentEmails);
    }

    [Fact]
    public async Task Run_PositivePositive_ConditionNotMet_StepSkipped()
    {
        WorkflowContext context = Order(giftWrap: false);

        await new WorkflowRunEngine().RunAsync(steps, context);

        Assert.DoesNotContain("GiftWrap", context.CompletedSteps);
        Assert.Contains("GiftWrap: not needed, skipped", context.History);
    }

    // ---------- compensation ----------

    [Fact]
    public async Task Run_PositiveNegative_LaterStepFails_EarlierStepsUndoneInReverse()
    {
        WorkflowContext context = Order(country: "XX");

        WorkflowResultModel result = await new WorkflowRunEngine().RunAsync(steps, context);

        Assert.Equal(WorkflowOutcome.RolledBack, result.Outcome);
        Assert.Equal("CreateShipment", result.FailedStep);
        Assert.Equal(["PAY-1"], gateway.Refunds);     // money given back
        Assert.Equal(10, warehouse.Stock["KB-01"]);    // stock released
        Assert.Empty(confirmation.SentEmails);         // never reached
        Assert.Equal(["ChargePayment: undone", "ReserveStock: undone"], context.History.Where(line => line.EndsWith("undone")));
    }

    [Fact]
    public async Task Run_PositiveNegative_RetriesUsedUp_OrFirstStepFails_RollsBack()
    {
        gateway.IsDown = true;
        WorkflowContext paymentDown = Order();
        WorkflowContext tooMany = Order(sku: "MS-01", quantity: 5);

        WorkflowResultModel downResult = await new WorkflowRunEngine().RunAsync(steps, paymentDown);
        WorkflowResultModel stockResult = await new WorkflowRunEngine().RunAsync(steps, tooMany);

        Assert.Equal(WorkflowOutcome.RolledBack, downResult.Outcome);
        Assert.Equal(3, gateway.Calls);                // tried MaximumAttempts times, then gave up
        Assert.Equal(10, warehouse.Stock["KB-01"]);
        Assert.Equal("ReserveStock", stockResult.FailedStep);
        Assert.Equal("Only 1 MS-01 in stock.", stockResult.Error);
    }

    // ---------- resume ----------

    [Fact]
    public async Task Resume_PositivePositive_FromSavedJson_FinishedStepsAreNotRepeated()
    {
        string? savedJson = null;
        var crashAfterPayment = new WorkflowRunEngine(context =>
        {
            savedJson = context.ToJson();
            return context.CompletedSteps.Contains("ChargePayment") ? throw new OperationCanceledException() : Task.CompletedTask;
        });
        await Assert.ThrowsAsync<OperationCanceledException>(() => crashAfterPayment.RunAsync(steps, Order()));

        WorkflowContext resumed = WorkflowContext.FromJson(savedJson!);
        WorkflowResultModel result = await new WorkflowRunEngine().RunAsync(steps, resumed);

        Assert.Equal(WorkflowOutcome.Completed, result.Outcome);
        Assert.Single(gateway.Charges);             // charged once, not twice
        Assert.Equal(8, warehouse.Stock["KB-01"]);   // reserved once
        Assert.Empty(gateway.Refunds);               // a crash is NOT a failure: nothing undone
    }

    [Fact]
    public async Task Resume_PositiveNegative_Cancelled_StopsWithoutCompensating()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new WorkflowRunEngine().RunAsync(steps, Order(), cancellation.Token));

        Assert.Empty(gateway.Refunds);
        Assert.Equal(10, warehouse.Stock["KB-01"]);
    }

    // ---------- broken input ----------

    [Fact]
    public async Task Run_NegativeNegative_MissingValueOrNulls_OrDuplicateNames_Rejected()
    {
        var noSku = new WorkflowContext();
        noSku.Values["Quantity"] = "1";

        WorkflowResultModel result = await new WorkflowRunEngine().RunAsync(steps, noSku);

        Assert.Equal(WorkflowOutcome.RolledBack, result.Outcome);
        Assert.Contains("'Sku' is missing", result.Error);
        await Assert.ThrowsAsync<ArgumentNullException>(() => new WorkflowRunEngine().RunAsync(null!, Order()));
        await Assert.ThrowsAsync<ArgumentNullException>(() => new WorkflowRunEngine().RunAsync(steps, null!));
        await Assert.ThrowsAsync<ArgumentException>(() => new WorkflowRunEngine().RunAsync([new GiftWrapStep(), new GiftWrapStep()], Order()));
    }

    [Fact]
    public void Resume_NegativeNegative_BrokenSavedState_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() => WorkflowContext.FromJson(""));
        Assert.ThrowsAny<ArgumentException>(() => WorkflowContext.FromJson("   "));
        Assert.ThrowsAny<System.Text.Json.JsonException>(() => WorkflowContext.FromJson("{ not json"));
        Assert.Throws<System.Text.Json.JsonException>(() => WorkflowContext.FromJson("null"));
    }
}
