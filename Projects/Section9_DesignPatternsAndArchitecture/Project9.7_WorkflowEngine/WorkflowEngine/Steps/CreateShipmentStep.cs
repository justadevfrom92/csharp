using WorkflowEngine.Contexts;
using WorkflowEngine.Services;

namespace WorkflowEngine.Steps;

public class CreateShipmentStep : BaseWorkflowStep
{
    private readonly WarehouseService warehouse;

    public CreateShipmentStep(WarehouseService warehouse)
    {
        this.warehouse = warehouse;
    }

    public override string Name => "CreateShipment";

    public override async Task ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken); // pretend to book a courier
        if (context.Values.GetValueOrDefault("Country") == "XX")
        {
            throw new InvalidOperationException("We don't ship to XX.");
        }
        warehouse.Shipments.Add($"{context.Get("Sku")} x{context.Get("Quantity")} for {context.Get("Customer")}");
    }

    public override Task CompensateAsync(WorkflowContext context)
    {
        warehouse.Shipments.RemoveAll(shipment => shipment.EndsWith(context.Get("Customer")));
        return Task.CompletedTask;
    }
}
