using Microsoft.EntityFrameworkCore;

// Fresh database every run so the demo always shows the same thing.
using (var database = new ShopDbContext())
{
    database.Database.EnsureDeleted();
    database.Database.EnsureCreated(); // creates tables + seed data (HasData)
}

PrintState("Start (seed data)");

// 1. A good checkout: everything is saved together.
Checkout(customerId: 1, productId: 1, quantity: 2);
PrintState("After buying 2 keyboards");

// 2. A checkout that fails at the LAST step (money already taken): NOTHING is saved.
Checkout(customerId: 1, productId: 2, quantity: 3);
PrintState("After failed mouse order (rolled back)");

// 3. Soft delete.
using (var database = new ShopDbContext())
{
    Product webcam = database.Products.Single(product => product.Name == "Old Webcam");
    database.Products.Remove(webcam); // our SaveChanges turns this into IsDeleted = true
    database.SaveChanges();
}
PrintState("After deleting Old Webcam");

using (var database = new ShopDbContext())
{
    Console.WriteLine("Still in the database (IgnoreQueryFilters):");
    foreach (Product product in database.Products.IgnoreQueryFilters().Where(product => product.IsDeleted))
    {
        Console.WriteLine($"  {product.Name} deleted at {product.DeletedAtUtc:u}");
    }

    // The old order line still points at its product, even if that product is later deleted.
    int ordersWithLines = database.Orders.Include(order => order.Lines).Count(order => order.Lines.Count > 0);
    Console.WriteLine($"Orders with lines: {ordersWithLines}");
}
return;

void Checkout(int customerId, int productId, int quantity)
{
    using var database = new ShopDbContext();
    using var transaction = database.Database.BeginTransaction();
    try
    {
        Customer customer = database.Customers.Single(existing => existing.Id == customerId);
        Product product = database.Products.Single(existing => existing.Id == productId);
        decimal total = product.Price * quantity;

        // Step 1: create the order and save it (gets an Id).
        var order = new Order { CustomerId = customer.Id, CreatedAtUtc = DateTime.UtcNow, Total = total };
        order.Lines.Add(new OrderLine { ProductId = product.Id, Quantity = quantity, UnitPrice = product.Price });
        database.Orders.Add(order);
        database.SaveChanges();

        // Step 2: take the money.
        if (customer.WalletBalance < total)
        {
            throw new InvalidOperationException($"{customer.Name} can't afford {total:0.00}");
        }
        customer.WalletBalance -= total;
        database.SaveChanges();

        // Step 3: reduce stock.
        if (product.Stock < quantity)
        {
            throw new InvalidOperationException($"Only {product.Stock} {product.Name} in stock");
        }
        product.Stock -= quantity;
        database.SaveChanges();

        transaction.Commit();
        Console.WriteLine($">> Checkout OK: order {order.Id}, total {total:0.00}");
    }
    catch (InvalidOperationException exception)
    {
        transaction.Rollback(); // undo steps 1 and 2 as well
        Console.WriteLine($">> Checkout FAILED, everything undone: {exception.Message}");
    }
}

void PrintState(string title)
{
    using var database = new ShopDbContext();
    Console.WriteLine($"--- {title} ---");
    foreach (Product product in database.Products.OrderBy(product => product.Id))
    {
        Console.WriteLine($"  {product.Name,-11} stock {product.Stock}");
    }
    Customer alice = database.Customers.Single();
    Console.WriteLine($"  Alice wallet: {alice.WalletBalance:0.00}, orders: {database.Orders.Count()}");
    Console.WriteLine();
}
