using EfCoreAdvancedQueries.DbContexts;
using EfCoreAdvancedQueries.Models;
using EfCoreAdvancedQueries.Outcomes;
using EfCoreAdvancedQueries.Services;
using Microsoft.EntityFrameworkCore;

// A fresh SQLite database file every run. LogTo prints every SQL statement EF sends, so you can SEE
// what each technique really does in the database.
DbContextOptions<ShopDbContext> options = new DbContextOptionsBuilder<ShopDbContext>()
    .UseSqlite("Data Source=advanced-queries.db")
    .LogTo(sql => Console.WriteLine("    SQL> " + sql.Split('\n', 2)[1].Trim().Replace("\n", " ").Replace("      ", " ")),
           new[] { DbLoggerCategory.Database.Command.Name }, Microsoft.Extensions.Logging.LogLevel.Information)
    .Options;

using (var setup = new ShopDbContext(options))
{
    setup.Database.EnsureDeleted();
    setup.Database.EnsureCreated();
}

await using var database = new ShopDbContext(options);
var service = new ProductQueryService(database);

Console.WriteLine("1. FromSql - search 'o' (the text becomes a parameter, not part of the SQL):");
foreach (ProductModel product in await service.SearchByNameAsync("o"))
{
    Console.WriteLine($"   {product.Name}");
}

Console.WriteLine("   ...and an injection attempt finds nothing, because it's just text:");
Console.WriteLine($"   {(await service.SearchByNameAsync("' OR 1=1 --")).Count} results");

Console.WriteLine("2. ExecuteSql - restock Furniture by 5:");
Console.WriteLine($"   {await service.RestockAsync("Furniture", 5)} row(s) changed");

Console.WriteLine("3. ExecuteUpdate - raise Computers prices by 10% in ONE statement:");
Console.WriteLine($"   {await service.RaisePricesAsync("Computers", 10)} row(s) changed");

Console.WriteLine("4. ExecuteDelete - delete discontinued Computers:");
Console.WriteLine($"   {await service.DeleteDiscontinuedAsync("Computers")} row(s) deleted");

Console.WriteLine("5. Compiled query - get product 1:");
ProductModel keyboard = (await service.GetByIdAsync(1))!;
Console.WriteLine($"   {keyboard.Name} now costs {keyboard.PriceInCents / 100m:0.00} (version {keyboard.Version})");

Console.WriteLine("6. Concurrency - Alice and Bob both opened the Keyboard at version " + keyboard.Version + ":");
PriceUpdateOutcome alice = await service.UpdatePriceAsync(1, 2999, keyboard.Version);
Console.WriteLine($"   Alice saves 29.99 -> {alice}");
PriceUpdateOutcome bob = await service.UpdatePriceAsync(1, 1999, keyboard.Version);
Console.WriteLine($"   Bob saves 19.99   -> {bob}");
Console.WriteLine($"   Final price: {(await service.GetByIdAsync(1))!.PriceInCents / 100m:0.00} (Alice's change was NOT lost)");
