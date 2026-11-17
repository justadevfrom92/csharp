using WorkflowEngine.Contexts;
using WorkflowEngine.Engines;
using WorkflowEngine.Models;
using WorkflowEngine.Outcomes;
using WorkflowEngine.Services;
using WorkflowEngine.Steps;
using WorkflowEngine.Variations;

namespace WorkflowEngine.Tests;

// The engine (original) vs the same process hand-written with try/catch (WorkflowEngine/Variations/FulfilOrderVariation.cs).
public class VariationsTest
{
    [Fact]
    public void HandWritten_PositivePositive_HappyPath_SameEffectsAsTheEngine()
    {
        var warehouse = new WarehouseService();
        var gateway = new PaymentGatewayService();

        string paymentId = FulfilOrderVariation.WithTryCatch(warehouse, gateway, "Ann", "KB-01", 2, 50m, "US");

        Assert.Equal("PAY-1", paymentId);
        Assert.Equal(8, warehouse.Stock["KB-01"]);
        Assert.Single(warehouse.Shipments);
    }

    [Fact]
    public async Task HandWritten_PositiveNegative_ShippingFails_UndoesLikeTheEngine()
    {
        var handWarehouse = new WarehouseService();
        var handGateway = new PaymentGatewayService();
        Assert.Throws<InvalidOperationException>(() => FulfilOrderVariation.WithTryCatch(handWarehouse, handGateway, "Ann", "KB-01", 2, 50m, "XX"));

        var engineWarehouse = new WarehouseService();
        var engineGateway = new PaymentGatewayService();
        var context = new WorkflowContext();
        foreach ((string key, string value) in new[] { ("Customer", "Ann"), ("Sku", "KB-01"), ("Quantity", "2"), ("Amount", "50.00"), ("Country", "XX") })
        {
            context.Values[key] = value;
        }
        WorkflowResultModel result = await new WorkflowRunEngine().RunAsync(
            [new ReserveStockStep(engineWarehouse), new ChargePaymentStep(engineGateway), new CreateShipmentStep(engineWarehouse)], context);

        Assert.Equal(WorkflowOutcome.RolledBack, result.Outcome);
        Assert.Equal(engineGateway.Refunds, handGateway.Refunds);                  // both refunded PAY-1
        Assert.Equal(engineWarehouse.Stock["KB-01"], handWarehouse.Stock["KB-01"]); // both released the stock
    }

    [Fact]
    public void HandWritten_PositiveNegative_CrashWithoutCheckpoints_ChargesTwice()
    {
        var warehouse = new WarehouseService();
        var gateway = new PaymentGatewayService();

        int charges = FulfilOrderVariation.ChargesAfterCrashAndRestartWithoutCheckpoints(warehouse, gateway);

        Assert.Equal(2, charges); // the engine's resume test charges ONCE (WorkflowRunEngineTest.Resume_PositivePositive...)
    }

    [Fact]
    public void HandWritten_NegativeNegative_NotEnoughStock_FailsAtStepOne_NothingToUndo()
    {
        var warehouse = new WarehouseService();
        var gateway = new PaymentGatewayService();

        Assert.Throws<InvalidOperationException>(() => FulfilOrderVariation.WithTryCatch(warehouse, gateway, "Ann", "MS-01", 5, 50m, "US"));

        Assert.Equal(0, gateway.Calls);
        Assert.Empty(gateway.Refunds);
    }
}
