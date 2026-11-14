using IdempotentMigrationScripts.DbContexts;
using IdempotentMigrationScripts.Models;
using IdempotentMigrationScripts.Services;
using Microsoft.EntityFrameworkCore;

// The app NEVER changes the database schema itself (no Database.Migrate()).
// Instead, at startup it checks that the DBA/pipeline already ran the migration script,
// and refuses to start if the database is behind.

string? connectionString = Environment.GetEnvironmentVariable("ORDERS_CONNECTION_STRING");

var options = new DbContextOptionsBuilder<OrdersDbContext>()
    .UseSqlServer(connectionString ?? "Server=localhost;Database=Orders;Trusted_Connection=True;TrustServerCertificate=True")
    .Options;
using var database = new OrdersDbContext(options);

Console.WriteLine("Migrations in this build:");
foreach (string migration in database.Database.GetMigrations())
{
    Console.WriteLine($"  {migration}");
}

if (connectionString == null)
{
    Console.WriteLine();
    Console.WriteLine("Set ORDERS_CONNECTION_STRING to check a real SQL Server database.");
    return 0;
}

List<string> pendingMigrations = database.Database.GetPendingMigrations().ToList();
if (pendingMigrations.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine($"STOP: the database is missing {pendingMigrations.Count} migration(s): {string.Join(", ", pendingMigrations)}");
    Console.WriteLine("Run migrations.sql against the database first (see README).");
    return 1; // non-zero exit = deployment fails loudly instead of running against an old schema
}

Console.WriteLine("Database schema is up to date. Starting app...");

// Stored procedure demo, for customer 1.
var procedures = new OrderProcedureService(database);
Console.WriteLine();
Console.WriteLine("Stored procedures for customer 1:");
List<OrderModel> orders = await procedures.GetOrdersForCustomerAsync(1);
Console.WriteLine($"  GetOrdersForCustomer  -> {orders.Count} order(s)");
foreach (OrderModel order in orders)
{
    Console.WriteLine($"    order {order.Id}: {order.Total:0.00} on {order.CreatedAtUtc:yyyy-MM-dd}");
}
Console.WriteLine($"  GetCustomerOrderTotal -> {await procedures.GetCustomerOrderTotalAsync(1):0.00}");
Console.WriteLine($"  ApplyCustomerDiscount -> {await procedures.ApplyCustomerDiscountAsync(1, 10)} order(s) got 10% off");
Console.WriteLine($"  GetCustomerOrderTotal -> {await procedures.GetCustomerOrderTotalAsync(1):0.00} after the discount");
return 0;
