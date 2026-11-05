using InventoryOrderAnalyticsSystem.Domain;
using InventoryOrderAnalyticsSystem.Inventory;

namespace InventoryOrderAnalyticsSystem.Orders;

/// <summary>
/// Orders are fulfilled strictly in arrival order. A Queue gives O(1) Enqueue/Dequeue at the
/// two ends, which a List cannot (List.RemoveAt(0) shifts every element: O(n)).
/// A Stack keeps an undo history of the most recent fulfilment (LIFO), which is exactly the
/// order in which reversals must happen.
/// </summary>
public sealed class FirstInFirstOutOrderProcessingPipeline(WarehouseInventoryRegistry inventoryRegistry)
{
    private readonly Queue<CustomerOrder> _pendingOrders = new();
    private readonly Stack<IReadOnlyList<FulfilledSaleRecord>> _fulfilmentUndoHistory = new();
    private readonly List<FulfilledSaleRecord> _fulfilledSalesLedger = [];

    public int PendingOrderCount => _pendingOrders.Count;
    public IReadOnlyList<FulfilledSaleRecord> FulfilledSalesLedger => _fulfilledSalesLedger;

    public void EnqueueOrder(CustomerOrder customerOrder) => _pendingOrders.Enqueue(customerOrder);

    public IReadOnlyList<OrderProcessingOutcome> ProcessAllPendingOrders()
    {
        var processingOutcomes = new List<OrderProcessingOutcome>(_pendingOrders.Count);
        while (_pendingOrders.TryDequeue(out CustomerOrder? nextOrder))
        {
            processingOutcomes.Add(ProcessSingleOrder(nextOrder));
        }
        return processingOutcomes;
    }

    /// <summary>Reverts the most recently fulfilled order (stock returned, sales removed).</summary>
    public bool TryUndoMostRecentFulfilment()
    {
        if (!_fulfilmentUndoHistory.TryPop(out IReadOnlyList<FulfilledSaleRecord>? reversedSales))
        {
            return false;
        }

        foreach (FulfilledSaleRecord reversedSale in reversedSales)
        {
            if (inventoryRegistry.TryFindStockItem(reversedSale.StockKeepingUnit, out InventoryStockItem? stockItem))
            {
                stockItem!.QuantityOnHand += reversedSale.QuantitySold;
            }
            _fulfilledSalesLedger.Remove(reversedSale);
        }
        return true;
    }

    private OrderProcessingOutcome ProcessSingleOrder(CustomerOrder customerOrder)
    {
        // Validate every line before mutating anything: an order is all-or-nothing.
        var resolvedLines = new List<(CustomerOrderLine Line, InventoryStockItem StockItem)>();
        foreach (CustomerOrderLine orderLine in customerOrder.OrderLines)
        {
            if (!inventoryRegistry.TryFindStockItem(orderLine.StockKeepingUnit, out InventoryStockItem? stockItem))
            {
                return new OrderProcessingOutcome(customerOrder, OrderFulfillmentStatus.RejectedUnknownProduct,
                    $"Unknown SKU {orderLine.StockKeepingUnit}");
            }
            if (stockItem!.QuantityOnHand < orderLine.RequestedQuantity)
            {
                return new OrderProcessingOutcome(customerOrder, OrderFulfillmentStatus.RejectedInsufficientStock,
                    $"{orderLine.StockKeepingUnit}: requested {orderLine.RequestedQuantity}, only {stockItem.QuantityOnHand} on hand");
            }
            resolvedLines.Add((orderLine, stockItem));
        }

        var salesForThisOrder = new List<FulfilledSaleRecord>(resolvedLines.Count);
        foreach ((CustomerOrderLine orderLine, InventoryStockItem stockItem) in resolvedLines)
        {
            stockItem.QuantityOnHand -= orderLine.RequestedQuantity;
            salesForThisOrder.Add(new FulfilledSaleRecord(customerOrder.OrderNumber, stockItem.StockKeepingUnit,
                stockItem.ProductCategoryName, orderLine.RequestedQuantity, stockItem.UnitPrice, customerOrder.PlacedAt));
        }

        _fulfilledSalesLedger.AddRange(salesForThisOrder);
        _fulfilmentUndoHistory.Push(salesForThisOrder);
        return new OrderProcessingOutcome(customerOrder, OrderFulfillmentStatus.Fulfilled,
            $"{salesForThisOrder.Count} line(s), {salesForThisOrder.Sum(sale => sale.LineRevenue):C2}");
    }
}
