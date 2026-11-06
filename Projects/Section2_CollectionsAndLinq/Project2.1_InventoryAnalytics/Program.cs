// This whole app is ONE file. The program runs from the top.
// The classes it uses (Product, Order, Sale) are defined at the BOTTOM of this file.
// (In C#, top-level code must come before any class definitions in the file.)

using System.Diagnostics;
using System.Globalization;

// Always show money as US dollars, whatever the computer's settings are.
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

Console.WriteLine("=== Warehouse Inventory & Orders ===");
Console.WriteLine();

// ---------- 1. Stock: Dictionary (find any product by its code instantly) ----------
var stockByProductCode = new Dictionary<string, Product>
{
    ["KB-01"] = new Product("KB-01", "Keyboard", "Computers", 25.00m, 40),
    ["MS-01"] = new Product("MS-01", "Mouse", "Computers", 12.50m, 3),
    ["MN-01"] = new Product("MN-01", "Monitor", "Computers", 180.00m, 8),
    ["DK-01"] = new Product("DK-01", "Desk", "Furniture", 220.00m, 2),
    ["CH-01"] = new Product("CH-01", "Chair", "Furniture", 95.00m, 15),
    ["LP-01"] = new Product("LP-01", "Desk Lamp", "Lighting", 30.00m, 0),
};

// ---------- 2. Categories: HashSet (no duplicates allowed) ----------
var categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (Product product in stockByProductCode.Values)
{
    categories.Add(product.Category);
}
bool addedAgain = categories.Add("computers"); // same as "Computers", ignoring case
Console.WriteLine($"Categories: {string.Join(", ", categories)}");
Console.WriteLine($"Tried to add 'computers' again -> added? {addedAgain}");
Console.WriteLine();

// ---------- 3. Orders: Queue (first in, first out) ----------
var waitingOrders = new Queue<Order>();
waitingOrders.Enqueue(new Order(1, "KB-01", 5));
waitingOrders.Enqueue(new Order(2, "MS-01", 10)); // not enough stock
waitingOrders.Enqueue(new Order(3, "XX-99", 1));  // unknown product
waitingOrders.Enqueue(new Order(4, "CH-01", 4));
waitingOrders.Enqueue(new Order(5, "MN-01", 6));

var completedSales = new List<Sale>();
var undoHistory = new Stack<Order>(); // Stack: last in, first out (used for "undo last order")

while (waitingOrders.Count > 0)
{
    Order order = waitingOrders.Dequeue();

    if (!stockByProductCode.TryGetValue(order.ProductCode, out Product? product))
    {
        Console.WriteLine($"Order {order.OrderNumber}: REJECTED - unknown product {order.ProductCode}");
        continue;
    }
    if (product.QuantityInStock < order.Quantity)
    {
        Console.WriteLine($"Order {order.OrderNumber}: REJECTED - only {product.QuantityInStock} {product.Name} left");
        continue;
    }

    product.QuantityInStock -= order.Quantity;
    completedSales.Add(new Sale(product.Code, product.Category, order.Quantity, order.Quantity * product.Price));
    undoHistory.Push(order);
    Console.WriteLine($"Order {order.OrderNumber}: OK - {order.Quantity} x {product.Name}");
}

// Undo the most recent order using the Stack.
Order lastOrder = undoHistory.Pop();
stockByProductCode[lastOrder.ProductCode].QuantityInStock += lastOrder.Quantity;
completedSales.RemoveAt(completedSales.Count - 1);
Console.WriteLine($"Undo: order {lastOrder.OrderNumber} cancelled, stock put back");
Console.WriteLine();

// ---------- 4. LINQ reports ----------
IEnumerable<Product> allProducts = stockByProductCode.Values;

decimal totalStockValue = allProducts.Sum(product => product.Price * product.QuantityInStock);
Console.WriteLine($"Total stock value: {totalStockValue:C}");

Console.WriteLine("Stock value per category:");
var valueByCategory = allProducts
    .GroupBy(product => product.Category)
    .Select(group => new { Category = group.Key, Value = group.Sum(product => product.Price * product.QuantityInStock) })
    .OrderByDescending(row => row.Value);
foreach (var row in valueByCategory)
{
    Console.WriteLine($"  {row.Category,-10} {row.Value,10:C}");
}

Console.WriteLine("Low stock (5 or less):");
foreach (Product product in allProducts.Where(product => product.QuantityInStock <= 5).OrderBy(product => product.QuantityInStock))
{
    Console.WriteLine($"  {product.Name,-10} {product.QuantityInStock} left");
}

Console.WriteLine("Sales report:");
foreach (var group in completedSales.GroupBy(sale => sale.Category))
{
    Console.WriteLine($"  {group.Key,-10} items: {group.Sum(sale => sale.Quantity),3}  revenue: {group.Sum(sale => sale.Total),10:C}");
}
if (completedSales.Count > 0)
{
    Console.WriteLine($"  Average sale: {completedSales.Average(sale => sale.Total):C}, biggest: {completedSales.Max(sale => sale.Total):C}, smallest: {completedSales.Min(sale => sale.Total):C}");
}
Console.WriteLine();

// ---------- 5. Deferred vs immediate execution ----------
IEnumerable<Product> outOfStockLater = allProducts.Where(product => product.QuantityInStock == 0); // nothing runs yet
List<Product> outOfStockNow = allProducts.Where(product => product.QuantityInStock == 0).ToList(); // runs right now
stockByProductCode["CH-01"].QuantityInStock = 0;
Console.WriteLine($"Deferred query sees the change:  {outOfStockLater.Count()} out of stock");
Console.WriteLine($"ToList() snapshot does not:      {outOfStockNow.Count} out of stock");
Console.WriteLine();

// ---------- 6. Dictionary vs List lookup speed ----------
CompareLookupSpeed();
return;

void CompareLookupSpeed()
{
    const int itemCount = 100_000;
    var list = new List<string>();
    var dictionary = new Dictionary<string, int>();
    for (int i = 0; i < itemCount; i++)
    {
        list.Add("SKU-" + i);
        dictionary["SKU-" + i] = i;
    }

    string lastCode = "SKU-" + (itemCount - 1);
    var timer = Stopwatch.StartNew();
    for (int i = 0; i < 1000; i++)
    {
        list.Contains(lastCode); // checks every item, one by one: O(n)
    }
    long listTime = timer.ElapsedMilliseconds;

    timer.Restart();
    for (int i = 0; i < 1000; i++)
    {
        dictionary.ContainsKey(lastCode); // jumps straight to it: O(1)
    }
    long dictionaryTime = timer.ElapsedMilliseconds;

    Console.WriteLine($"1000 lookups in {itemCount:N0} items -> List: {listTime} ms, Dictionary: {dictionaryTime} ms");
}

class Product
{
    public Product(string code, string name, string category, decimal price, int quantityInStock)
    {
        Code = code;
        Name = name;
        Category = category;
        Price = price;
        QuantityInStock = quantityInStock;
    }

    public string Code { get; }
    public string Name { get; }
    public string Category { get; }
    public decimal Price { get; }
    public int QuantityInStock { get; set; }
}

record Order(int OrderNumber, string ProductCode, int Quantity);

record Sale(string ProductCode, string Category, int Quantity, decimal Total);
