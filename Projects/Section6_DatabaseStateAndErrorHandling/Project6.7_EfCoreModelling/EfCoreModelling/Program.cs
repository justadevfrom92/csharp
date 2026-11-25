using EfCoreModelling.DbContexts;
using EfCoreModelling.Models;
using EfCoreModelling.Seeders;
using EfCoreModelling.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

// In-memory SQLite (one open connection keeps it alive). LogTo prints each SQL command, so you can SEE the
// Type column, the WHERE from OfType, the JOIN, the GROUP BY and what AutoInclude adds.
using var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();
bool showSql = false;
DbContextOptions<ShopDbContext> options = new DbContextOptionsBuilder<ShopDbContext>()
    .UseSqlite(connection)
    .LogTo(message =>
    {
        if (showSql)
        {
            Console.WriteLine("    SQL> " + string.Join(" ", message.Split('\n').Skip(1).Select(line => line.Trim())));
        }
    }, [DbLoggerCategory.Database.Command.Name], LogLevel.Information)
    .Options;

Guid firstInvoiceId;
using (var setup = new ShopDbContext(options))
{
    setup.Database.EnsureCreated();
    firstInvoiceId = ShopSeeder.Seed(setup).AcmeInvoiceId;
}

using var database = new ShopDbContext(options);
var service = new ProductSalesService(database);

Console.WriteLine("1. One table for both kinds (table-per-hierarchy), with a Type column:");
// EF.Property<T>(...) reads SHADOW columns - only inside a query (on a loaded object it throws).
var rows = database.Products
    .Select(product => new { Id = EF.Property<int>(product, "Id"), Type = EF.Property<string>(product, "Type"), product.Name })
    .ToList();
foreach (var row in rows)
{
    Console.WriteLine($"   {row.Id}  {row.Type,-8}  {row.Name}");
}

showSql = true;
Console.WriteLine("2. Material sold, all computed in the database:");
foreach (ProductConsumptionModel row in await service.GetMaterialConsumptionAsync())
{
    Console.WriteLine($"   {row.Name}: {row.AmountSold}");
}

Console.WriteLine("3. Loading one invoice - AutoInclude brings its lines without Include():");
InvoiceModel invoice = (await service.FindInvoiceAsync(firstInvoiceId))!;
Console.WriteLine($"   {invoice.Customer}: {invoice.Lines.Count} lines");

Console.WriteLine("4. A list page with IgnoreAutoIncludes - no lines loaded:");
foreach (InvoiceModel listed in await service.ListInvoicesWithoutLinesAsync())
{
    Console.WriteLine($"   {listed.Customer}: {listed.Lines.Count} lines loaded");
}
showSql = false;

Console.WriteLine("5. Lines can only be added through the invoice:");
try
{
    ((ICollection<InvoiceLineModel>)invoice.Lines).Add(new InvoiceLineModel(Guid.NewGuid(), 1m));
}
catch (NotSupportedException)
{
    Console.WriteLine("   invoice.Lines is read-only -> NotSupportedException");
}
