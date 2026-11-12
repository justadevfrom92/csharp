using EfCoreAdvancedQueries.DbContexts;
using EfCoreAdvancedQueries.Models;
using EfCoreAdvancedQueries.Outcomes;
using Microsoft.EntityFrameworkCore;

namespace EfCoreAdvancedQueries.Services;

public class ProductQueryService
{
    private readonly ShopDbContext database;

    public ProductQueryService(ShopDbContext database)
    {
        this.database = database;
    }

    // ---------- 1. RAW SQL QUERY: FromSql ----------
    // FromSql takes an INTERPOLATED string, but does NOT paste the values into the SQL text: every {value}
    // becomes a database parameter (@p0). So "' OR 1=1 --" is searched for as plain text - SQL injection
    // is impossible. (FromSqlRaw("... '" + text + "'") WOULD be injectable - never build SQL by joining strings.)
    // The result is still an IQueryable, so LINQ (OrderBy) can be added and runs in the database too.
    public Task<List<ProductModel>> SearchByNameAsync(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string pattern = "%" + text + "%";
        return database.Products
            .FromSql($"SELECT * FROM Products WHERE Name LIKE {pattern}")
            .OrderBy(product => product.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    // ---------- 2. RAW SQL COMMAND: ExecuteSql ----------
    // For SQL that changes data and returns no rows. Same parameter safety as FromSql. Returns rows changed.
    public Task<int> RestockAsync(string category, int extraUnits)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        if (extraUnits <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(extraUnits), "Must add at least 1 unit.");
        }
        return database.Database.ExecuteSqlAsync(
            $"UPDATE Products SET Stock = Stock + {extraUnits}, Version = Version + 1 WHERE Category = {category} AND IsDiscontinued = 0");
    }

    // ---------- 3. BULK UPDATE: ExecuteUpdate ----------
    // ONE "UPDATE ... SET ... WHERE ..." in the database. No rows are loaded into memory, no SaveChanges.
    // 10,000 products = still one statement (loading them, changing them and saving would be 10,000 UPDATEs).
    // Careful: it bypasses the change tracker - objects ALREADY loaded in this DbContext keep their old values.
    public Task<int> RaisePricesAsync(string category, int percent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        if (percent < 1 || percent > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), "Raise by 1 to 100 percent.");
        }
        return database.Products
            .Where(product => product.Category == category && !product.IsDiscontinued)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(product => product.PriceInCents, product => product.PriceInCents * (100 + percent) / 100)
                .SetProperty(product => product.Version, product => product.Version + 1));
    }

    // ---------- 4. BULK DELETE: ExecuteDelete ----------
    public Task<int> DeleteDiscontinuedAsync(string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        return database.Products
            .Where(product => product.Category == category && product.IsDiscontinued)
            .ExecuteDeleteAsync();
    }

    // ---------- 5. COMPILED QUERY ----------
    // EF normally turns a LINQ query into SQL every time it runs (cached, but still some work per call).
    // A compiled query does that translation ONCE, when this static field is created. Worth it for small
    // queries that run very often (like "get by id" on every request).
    private static readonly Func<ShopDbContext, int, Task<ProductModel?>> findByIdCompiled =
        EF.CompileAsyncQuery((ShopDbContext context, int id) => context.Products.AsNoTracking().FirstOrDefault(product => product.Id == id));

    public Task<ProductModel?> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Id must be more than 0.");
        }
        return findByIdCompiled(database, id);
    }

    // ---------- 6. CONCURRENCY: two people editing the same product ----------
    // The caller says which Version it SAW when it showed the product to the user. If the row has changed
    // since then, the save is refused instead of overwriting the other person's change ("lost update").
    public async Task<PriceUpdateOutcome> UpdatePriceAsync(int id, long newPriceInCents, int versionTheUserSaw)
    {
        if (newPriceInCents <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newPriceInCents), "Price must be more than 0.");
        }

        ProductModel? product = await database.Products.FirstOrDefaultAsync(existing => existing.Id == id);
        if (product == null)
        {
            return PriceUpdateOutcome.NotFound;
        }

        // Tell EF "the version I'm basing this change on is the one the USER saw", so its WHERE Version = ...
        // check catches changes made both before AND after we loaded the row just now.
        database.Entry(product).Property(existing => existing.Version).OriginalValue = versionTheUserSaw;
        product.PriceInCents = newPriceInCents;
        product.Version = versionTheUserSaw + 1;

        try
        {
            await database.SaveChangesAsync();
            return PriceUpdateOutcome.Updated;
        }
        catch (DbUpdateConcurrencyException)
        {
            // 0 rows matched "WHERE Id = .. AND Version = ..": someone else changed it first.
            database.ChangeTracker.Clear(); // forget our rejected change
            return PriceUpdateOutcome.ConflictSomeoneElseChangedIt;
        }
    }
}
