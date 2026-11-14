using WorkflowEngine.Contexts;
using WorkflowEngine.Services;

namespace WorkflowEngine.Steps;

public class ReserveStockStep : BaseWorkflowStep
{
    private readonly WarehouseService warehouse;

    public ReserveStockStep(WarehouseService warehouse)
    {
        this.warehouse = warehouse;
    }

    public override string Name => "ReserveStock";

    public override Task ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken)
    {
        warehouse.Reserve(context.Get("Sku"), int.Parse(context.Get("Quantity")));
        return Task.CompletedTask;
    }

    public override Task CompensateAsync(WorkflowContext context)
    {
        warehouse.Release(context.Get("Sku"), int.Parse(context.Get("Quantity")));
        return Task.CompletedTask;
    }
}
