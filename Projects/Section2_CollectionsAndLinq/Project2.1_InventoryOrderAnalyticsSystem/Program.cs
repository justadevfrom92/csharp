using System.Globalization;
using InventoryOrderAnalyticsSystem.Analytics;
using InventoryOrderAnalyticsSystem.Domain;
using InventoryOrderAnalyticsSystem.Inventory;
using InventoryOrderAnalyticsSystem.Orders;

CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

var inventoryRegistry = new WarehouseInventoryRegistry();
SeedWarehouseInventory(inventoryRegistry);

PrintSectionHeader("1. Inventory registered (Dictionary<StockKeepingUnitCode, InventoryStockItem>)");
Console.WriteLine($"Distinct SKUs: {inventoryRegistry.DistinctStockItemCount}");
Console.WriteLine($"Unique categories (HashSet): {string.Join(", ", inventoryRegistry.RegisteredProductCategories.Order())}");
var lookupSku = new StockKeepingUnitCode("elec-001"); // normalised to ELEC-001
if (inventoryRegistry.TryFindStockItem(lookupSku, out InventoryStockItem? foundItem))
{
    Console.WriteLine($"O(1) lookup of '{lookupSku}': {foundItem!.ProductDisplayName}, {foundItem.QuantityOnHand} on hand");
}
Console.WriteLine($"Add duplicate tag 'WIRELESS' to ELEC-001 -> added? {inventoryRegistry.TryAddDescriptiveTag(lookupSku, "WIRELESS")}");
Console.WriteLine($"Add new tag 'bestseller' to ELEC-001  -> added? {inventoryRegistry.TryAddDescriptiveTag(lookupSku, "bestseller")}");

PrintSectionHeader("2. Inventory valuation before orders (LINQ GroupBy / Sum)");
PrintInventoryValuation(inventoryRegistry);

PrintSectionHeader("3. Order processing pipeline (Queue<CustomerOrder>, FIFO)");
var orderPipeline = new FirstInFirstOutOrderProcessingPipeline(inventoryRegistry);
foreach (CustomerOrder customerOrder in CreateSampleOrders())
{
    orderPipeline.EnqueueOrder(customerOrder);
}
Console.WriteLine($"Pending orders: {orderPipeline.PendingOrderCount}");
foreach (OrderProcessingOutcome outcome in orderPipeline.ProcessAllPendingOrders())
{
    Console.WriteLine($"  Order #{outcome.Order.OrderNumber,-3} {outcome.Order.CustomerName,-10} {outcome.Status,-26} {outcome.Explanation}");
}

Console.WriteLine($"Undo most recent fulfilment (Stack<T>, LIFO) -> {orderPipeline.TryUndoMostRecentFulfilment()}");
Console.WriteLine($"Sales ledger lines after undo: {orderPipeline.FulfilledSalesLedger.Count}");

PrintSectionHeader("4. Low-stock alerts (Where / OrderBy / Select)");
foreach (LowStockAlert lowStockAlert in InventoryAndSalesLinqAnalytics.FindLowStockItems(inventoryRegistry.AllStockItems))
{
    Console.WriteLine($"  {lowStockAlert.StockKeepingUnit,-9} {lowStockAlert.ProductDisplayName,-26} on hand {lowStockAlert.QuantityOnHand,3} / threshold {lowStockAlert.ReorderThresholdQuantity,3} -> reorder {lowStockAlert.SuggestedReorderQuantity}");
}

PrintSectionHeader("5. Sales report by category (GroupBy / Sum / Average / Max / MaxBy)");
Console.WriteLine($"  {"Category",-12} {"Orders",6} {"Units",6} {"Revenue",12} {"Avg line",10} {"Max line",10}  Best seller");
foreach (CategorySalesReport categoryReport in InventoryAndSalesLinqAnalytics.BuildSalesReportByCategory(orderPipeline.FulfilledSalesLedger))
{
    Console.WriteLine($"  {categoryReport.CategoryName,-12} {categoryReport.OrderCount,6} {categoryReport.TotalUnitsSold,6} {categoryReport.TotalRevenue,12:C2} {categoryReport.AverageLineRevenue,10:C2} {categoryReport.LargestSingleLineRevenue,10:C2}  {categoryReport.BestSellingStockKeepingUnit}");
}

PrintSectionHeader("6. Inventory valuation after orders");
PrintInventoryValuation(inventoryRegistry);
Console.WriteLine("Tag usage (SelectMany + GroupBy): " + string.Join(", ",
    InventoryAndSalesLinqAnalytics.CountProductsPerTag(inventoryRegistry.AllStockItems).Select(tagCount => $"{tagCount.TagName}={tagCount.ProductCount}")));

PrintSectionHeader("7. Deferred vs immediate execution");
DemonstrateDeferredVersusImmediateExecution(inventoryRegistry);

PrintSectionHeader("8. Collection interfaces: IEnumerable / ICollection / IList / IQueryable");
DemonstrateCollectionInterfaces(inventoryRegistry);

PrintSectionHeader("9. Lookup cost: List scan O(n) vs Dictionary O(1)");
foreach (int catalogSize in new[] { 1_000, 10_000, 50_000 })
{
    LookupBenchmarkResult benchmarkResult = CollectionLookupPerformanceComparison.CompareListScanWithDictionaryLookup(catalogSize, lookupCount: 5_000);
    Console.WriteLine($"  {benchmarkResult.CatalogSize,7:N0} items x {benchmarkResult.LookupCount:N0} lookups: list {benchmarkResult.LinearListScanDuration.TotalMilliseconds,9:N2} ms | dictionary {benchmarkResult.DictionaryHashLookupDuration.TotalMilliseconds,6:N3} ms | ~{benchmarkResult.SpeedupFactor:N0}x faster");
}

return 0;

static void SeedWarehouseInventory(WarehouseInventoryRegistry registry)
{
    InventoryStockItem CreateItem(string sku, string name, string category, decimal price, int onHand, int threshold) => new()
    {
        StockKeepingUnit = new StockKeepingUnitCode(sku),
        ProductDisplayName = name,
        ProductCategoryName = category,
        UnitPrice = price,
        QuantityOnHand = onHand,
        ReorderThresholdQuantity = threshold
    };

    registry.RegisterStockItem(CreateItem("ELEC-001", "Wireless Mouse", "Electronics", 24.99m, 120, 30), "wireless", "Wireless", "accessory");
    registry.RegisterStockItem(CreateItem("ELEC-002", "Mechanical Keyboard", "Electronics", 89.50m, 40, 15), "accessory", "rgb");
    registry.RegisterStockItem(CreateItem("ELEC-003", "27\" 4K Monitor", "Electronics", 329.00m, 12, 10), "fragile", "display");
    registry.RegisterStockItem(CreateItem("HOME-001", "Ceramic Mug", "Kitchen", 8.75m, 300, 50), "fragile", "dishwasher-safe");
    registry.RegisterStockItem(CreateItem("HOME-002", "Chef's Knife", "Kitchen", 64.00m, 25, 10), "sharp");
    registry.RegisterStockItem(CreateItem("OFFC-001", "A5 Notebook", "Office", 4.20m, 500, 100), "paper");
    registry.RegisterStockItem(CreateItem("OFFC-002", "Gel Pen 10-pack", "Office", 6.99m, 60, 40), "paper", "PAPER");
    registry.RegisterStockItem(CreateItem("OFFC-003", "Standing Desk", "office", 449.00m, 3, 2), "heavy", "assembly-required");
}

static IEnumerable<CustomerOrder> CreateSampleOrders()
{
    var baseTime = new DateTimeOffset(2026, 11, 2, 9, 0, 0, TimeSpan.Zero);
    StockKeepingUnitCode Sku(string code) => new(code);
    yield return new CustomerOrder(1, "Avery", baseTime, [new(Sku("ELEC-001"), 4), new(Sku("OFFC-001"), 50)]);
    yield return new CustomerOrder(2, "Blake", baseTime.AddMinutes(5), [new(Sku("ELEC-003"), 3)]);
    yield return new CustomerOrder(3, "Casey", baseTime.AddMinutes(9), [new(Sku("HOME-002"), 30)]);            // insufficient stock
    yield return new CustomerOrder(4, "Devon", baseTime.AddMinutes(12), [new(Sku("ELEC-002"), 26), new(Sku("HOME-001"), 260)]);
    yield return new CustomerOrder(5, "Emery", baseTime.AddMinutes(15), [new(Sku("TOYS-404"), 1)]);            // unknown product
    yield return new CustomerOrder(6, "Finley", baseTime.AddMinutes(20), [new(Sku("OFFC-002"), 25), new(Sku("ELEC-001"), 2)]);
    yield return new CustomerOrder(7, "Gray", baseTime.AddMinutes(30), [new(Sku("OFFC-003"), 1)]);
}

static void PrintInventoryValuation(WarehouseInventoryRegistry registry)
{
    foreach (CategoryInventoryValuation valuation in InventoryAndSalesLinqAnalytics.CalculateInventoryValueByCategory(registry.AllStockItems))
    {
        Console.WriteLine($"  {valuation.CategoryName,-12} {valuation.DistinctProductCount} products {valuation.TotalUnitsOnHand,5} units {valuation.TotalStockValue,12:C2}");
    }
    Console.WriteLine($"  {"TOTAL",-12} {InventoryAndSalesLinqAnalytics.CalculateTotalInventoryValue(registry.AllStockItems),37:C2}");
}

static void DemonstrateDeferredVersusImmediateExecution(WarehouseInventoryRegistry registry)
{
    int predicateEvaluationCount = 0;

    // Deferred: building the query runs nothing.
    IEnumerable<InventoryStockItem> deferredExpensiveItemsQuery = registry.AllStockItems.Where(stockItem =>
    {
        predicateEvaluationCount++;
        return stockItem.UnitPrice > 50m;
    });
    Console.WriteLine($"  After defining the query, predicate ran {predicateEvaluationCount} times");

    // Immediate: ToList() enumerates once and captures a snapshot.
    List<InventoryStockItem> immediateSnapshot = deferredExpensiveItemsQuery.ToList();
    Console.WriteLine($"  After ToList(), predicate ran {predicateEvaluationCount} times; snapshot has {immediateSnapshot.Count} items");

    registry.RegisterStockItem(new InventoryStockItem
    {
        StockKeepingUnit = new StockKeepingUnitCode("ELEC-900"),
        ProductDisplayName = "Noise-cancelling Headphones",
        ProductCategoryName = "Electronics",
        UnitPrice = 199m,
        QuantityOnHand = 10,
        ReorderThresholdQuantity = 5
    });

    // Re-enumerating the deferred query sees the new item AND re-runs the predicate for every element.
    int deferredCountAfterInsert = deferredExpensiveItemsQuery.Count();
    Console.WriteLine($"  After adding an item: deferred query -> {deferredCountAfterInsert} items, snapshot -> {immediateSnapshot.Count} items");
    Console.WriteLine($"  Predicate has now run {predicateEvaluationCount} times in total (deferred queries re-execute on every enumeration)");
    InventoryStockItem[] arraySnapshot = deferredExpensiveItemsQuery.ToArray();
    Console.WriteLine($"  ToArray() snapshot length: {arraySnapshot.Length}");
}

static void DemonstrateCollectionInterfaces(WarehouseInventoryRegistry registry)
{
    IEnumerable<InventoryStockItem> forwardOnlySequence = registry.AllStockItems;           // only iteration
    ICollection<InventoryStockItem> countableCollection = forwardOnlySequence.ToList();       // + Count, Add, Remove, Contains
    IList<InventoryStockItem> indexableList = (IList<InventoryStockItem>)countableCollection; // + indexer, Insert, IndexOf
    Console.WriteLine($"  IEnumerable: iterate only | ICollection.Count = {countableCollection.Count} | IList[0] = {indexableList[0].StockKeepingUnit}");

    // IQueryable builds an expression tree that a provider (e.g. EF Core) translates; here we print it.
    IQueryable<InventoryStockItem> queryableInventory = registry.AllStockItems.AsQueryable()
        .Where(stockItem => stockItem.QuantityOnHand < 20)
        .OrderBy(stockItem => stockItem.UnitPrice);
    Console.WriteLine($"  IQueryable expression tree: {queryableInventory.Expression}");
    Console.WriteLine($"  IQueryable result: {string.Join(", ", queryableInventory.Select(stockItem => stockItem.StockKeepingUnit.Value))}");
}

static void PrintSectionHeader(string title)
{
    Console.WriteLine();
    Console.WriteLine($"=== {title} ===");
}
