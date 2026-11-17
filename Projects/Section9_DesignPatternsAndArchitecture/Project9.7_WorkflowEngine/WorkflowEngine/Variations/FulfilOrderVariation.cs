using WorkflowEngine.Services;

namespace WorkflowEngine.Variations;

// The order process WITHOUT the engine: one method, the steps written out, compensation in catch blocks.
// Perfectly readable for ONE short process. The engine (original) wins when there are many processes, or when you
// need retries, conditional steps, checkpoints and resume - each of which this version would have to hand-write again.
public static class FulfilOrderVariation
{
    public static string WithTryCatch(WarehouseService warehouse, PaymentGatewayService gateway, string customer, string sku, int quantity, decimal amount, string country)
    {
        warehouse.Reserve(sku, quantity); // if this fails, nothing to undo yet
        string paymentId;
        try
        {
            paymentId = gateway.Charge(customer, amount);
        }
        catch
        {
            warehouse.Release(sku, quantity);  // undo step 1
            throw;
        }
        try
        {
            if (country == "XX")
            {
                throw new InvalidOperationException("We don't ship to XX.");
            }
            warehouse.Shipments.Add($"{sku} x{quantity} for {customer}");
        }
        catch
        {
            gateway.Refund(paymentId);         // undo step 2...
            warehouse.Release(sku, quantity);  // ...then step 1, newest first
            throw;
        }
        return paymentId;
    }

    // What a crash does WITHOUT checkpoints: the process dies after charging, and the restart begins again
    // from the top - so the customer is charged TWICE. (The engine saves CompletedSteps and skips them on resume.)
    public static int ChargesAfterCrashAndRestartWithoutCheckpoints(WarehouseService warehouse, PaymentGatewayService gateway)
    {
        try
        {
            warehouse.Reserve("KB-01", 1);
            gateway.Charge("Ann", 25m);
            throw new OperationCanceledException("power cut"); // crash before shipping
        }
        catch (OperationCanceledException)
        {
        }
        WithTryCatch(warehouse, gateway, "Ann", "KB-01", 1, 25m, "US"); // restart from scratch
        return gateway.Charges.Count;
    }
}
