# Cheat Sheet — Section 6: Database, State & Error Handling

Projects: [6.1](../Section6_DatabaseStateAndErrorHandling/Project6.1_EfCoreQueryLogging), [6.2](../Section6_DatabaseStateAndErrorHandling/Project6.2_TransactionalCheckout), [6.3](../Section6_DatabaseStateAndErrorHandling/Project6.3_SharedCacheAndSettings), [6.4](../Section6_DatabaseStateAndErrorHandling/Project6.4_SessionShoppingCart), [6.5](../Section6_DatabaseStateAndErrorHandling/Project6.5_ErrorHandlingAndCrashRecovery), [6.6](../Section6_DatabaseStateAndErrorHandling/Project6.6_EfCoreAdvancedQueries)

## EF Core: set up

```csharp
public class ShopDbContext : DbContext
{
    public ShopDbContext(DbContextOptions<ShopDbContext> options) : base(options)
    {
    }

    public DbSet<ProductModel> Products { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductModel>().Property(product => product.Name).HasMaxLength(100).IsRequired();
        modelBuilder.Entity<ProductModel>().Property(product => product.Price).HasPrecision(18, 2);
        modelBuilder.Entity<ProductModel>().HasIndex(product => product.Sku).IsUnique();
        modelBuilder.Entity<ProductModel>().HasData(new ProductModel { Id = 1, Name = "Keyboard", Sku = "KB-01" });
    }
}

builder.Services.AddDbContext<ShopDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Shop")));     // or UseSqlServer(...)
```

```json
"ConnectionStrings": {
  "Shop": "Data Source=shop.db"
}
```

## EF Core: CRUD

```csharp
// Create
database.Products.Add(new ProductModel { Name = "Mouse" });
await database.SaveChangesAsync();

// Read
ProductModel? one = await database.Products.FindAsync(1);
List<ProductModel> cheap = await database.Products
    .Where(product => product.Price < 20)
    .OrderBy(product => product.Name)
    .AsNoTracking()                     // read-only: faster
    .ToListAsync();
bool exists = await database.Products.AnyAsync(product => product.Sku == "KB-01");
int count = await database.Products.CountAsync();

// Update (tracked: just change and save)
ProductModel product = await database.Products.SingleAsync(existing => existing.Id == 1);
product.Price = 29.99m;
await database.SaveChangesAsync();

// Delete
database.Products.Remove(product);
await database.SaveChangesAsync();

// Bulk, without loading (one SQL statement)
await database.Products.Where(product => product.Stock == 0)
    .ExecuteUpdateAsync(setters => setters.SetProperty(product => product.IsHidden, true));
await database.Products.Where(product => product.IsDeleted).ExecuteDeleteAsync();
```

## EF Core: related data

```csharp
modelBuilder.Entity<BookModel>()
    .HasOne(book => book.Author).WithMany(author => author.Books).HasForeignKey(book => book.AuthorId); // one-to-many
modelBuilder.Entity<BookModel>()
    .HasMany(book => book.Tags).WithMany(tag => tag.Books);                                              // many-to-many

List<BookModel> books = await database.Books
    .Include(book => book.Author)
    .ThenInclude(author => author.Profile)
    .Include(book => book.Tags)
    .ToListAsync();

var titles = await database.Books
    .Select(book => new { book.Title, AuthorName = book.Author.Name }) // only the columns you need
    .ToListAsync();

string sql = database.Books.Where(book => book.Year > 2000).ToQueryString(); // see the SQL
```

## EF Core: raw SQL (always parameterized)

```csharp
List<ProductModel> found = await database.Products
    .FromSql($"SELECT * FROM Products WHERE Name LIKE {pattern}")   // {pattern} becomes @p0
    .ToListAsync();

int changed = await database.Database.ExecuteSqlAsync($"UPDATE Products SET Stock = 0 WHERE Id = {id}");

List<string> names = await database.Database
    .SqlQuery<string>($"SELECT Name AS Value FROM Products")       // non-entity results
    .ToListAsync();
```

Never `FromSqlRaw("... '" + text + "'")` — joining strings into SQL allows SQL injection.

## Stored procedures (SQL Server)

A **stored procedure** is a named piece of SQL saved **inside the database**, called like a function.

```sql
CREATE PROCEDURE GetProductsByCategory
    @Category NVARCHAR(50)
AS
BEGIN
    SELECT Id, Name, Price FROM Products WHERE Category = @Category;
END
```

```csharp
// returns rows -> map them to entities
List<ProductModel> computers = await database.Products
    .FromSql($"EXEC GetProductsByCategory @Category = {category}")
    .ToListAsync();

// changes data, returns no rows
await database.Database.ExecuteSqlAsync($"EXEC ArchiveOldOrders @OlderThanDays = {days}");
```

Add the procedure itself in a migration. Wrap it in `EXEC(N'...')` (single quotes doubled inside), because
SQL Server only allows `CREATE PROCEDURE` at the start of a batch and idempotent scripts put it inside `IF ... BEGIN`:

```csharp
migrationBuilder.Sql(@"EXEC(N'
CREATE OR ALTER PROCEDURE GetProductsByCategory @Category NVARCHAR(50)
AS
BEGIN
    SELECT Id, Name, Price FROM Products WHERE Category = @Category;
END');");
```

Output parameter:

```csharp
var totalParameter = new SqlParameter("@Total", SqlDbType.Decimal) { Direction = ParameterDirection.Output, Precision = 18, Scale = 2 };
await database.Database.ExecuteSqlAsync($"EXEC GetCustomerOrderTotal @CustomerId = {customerId}, @Total = {totalParameter} OUTPUT");
decimal total = (decimal)totalParameter.Value;
```

Working example: [Project 7.7](../Section7_SecurityBackgroundTasksAndDeployment/Project7.7_IdempotentMigrationScripts).

## EF Core: transactions and concurrency

```csharp
await using var transaction = await database.Database.BeginTransactionAsync();
try
{
    // several SaveChangesAsync() calls
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}

modelBuilder.Entity<ProductModel>().Property(product => product.Version).IsConcurrencyToken();
try
{
    await database.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException)
{
    // someone else changed the row first
}
```

## EF Core: global filters, compiled queries, logging

```csharp
modelBuilder.Entity<ProductModel>().HasQueryFilter(product => !product.IsDeleted);
database.Products.IgnoreQueryFilters();

private static readonly Func<ShopDbContext, int, Task<ProductModel?>> findById =
    EF.CompileAsyncQuery((ShopDbContext context, int id) => context.Products.FirstOrDefault(product => product.Id == id));

options.LogTo(Console.WriteLine, LogLevel.Information);   // print SQL
options.EnableSensitiveDataLogging();                       // show parameter values (development only)
```

## Settings (`appsettings.json` → class)

```csharp
builder.Services.AddOptions<ShopSettings>()
    .Bind(builder.Configuration.GetSection("Shop"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

public ShopService(IOptions<ShopSettings> settings)          // read once
public FlagService(IOptionsMonitor<FlagSettings> flags)      // always latest

string? value = builder.Configuration["Shop:ShopName"];
int minutes = builder.Configuration.GetValue<int>("Shop:CacheMinutes");
```

Order (later wins): `appsettings.json` → `appsettings.{Environment}.json` → user secrets (Development) → environment variables (`Shop__ShopName`) → command line.

## Caching in memory

```csharp
builder.Services.AddMemoryCache();

List<string>? categories = await cache.GetOrCreateAsync("categories", async entry =>
{
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
    return await LoadCategoriesAsync();
});
cache.Remove("categories");
```

## Session

```csharp
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
});
app.UseSession();

HttpContext.Session.SetString("cart", JsonSerializer.Serialize(cart));
string? json = HttpContext.Session.GetString("cart");
HttpContext.Session.Clear();
```

## Logging

```csharp
public OrderService(ILogger<OrderService> logger)
{
    this.logger = logger;
}

logger.LogInformation("Order {OrderId} placed by {Customer}", orderId, customer); // structured: placeholders, not $""
logger.LogWarning("Stock low for {Sku}", sku);
logger.LogError(exception, "Payment failed for {OrderId}", orderId);
```

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning"
  }
}
```

## Error handling

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/Error/Status/{0}");
```

```csharp
public class InsufficientStockException : Exception
{
    public InsufficientStockException(string sku, int requested)
        : base($"Not enough {sku} for {requested}.")
    {
    }
}
```

Catch only what you can handle; let the rest reach the global handler; never show stack traces to users.
