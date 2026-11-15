using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using EfCoreAdvancedQueries.DbContexts;
using EfCoreAdvancedQueries.Models;

namespace EfCoreAdvancedQueries.Variations;

// Other ways to write what ProductQueryService does - including two that are WRONG on purpose, so the tests
// can show exactly what goes wrong.
public class ProductQueryVariation
{
    private readonly ShopDbContext database;

    public ProductQueryVariation(ShopDbContext database)
    {
        this.database = database;
    }

    // ---------- raw SQL search ----------

    // FromSqlInterpolated: the older name for FromSql. Identical: every {value} becomes a parameter.
    public Task<List<ProductModel>> SearchWithFromSqlInterpolated(string text)
    {
        string pattern = "%" + text + "%";
        return database.Products.FromSqlInterpolated($"SELECT * FROM Products WHERE Name LIKE {pattern}").AsNoTracking().ToListAsync();
    }

    // FromSqlRaw WITH parameters: a plain string with a placeholder + a real parameter object. Also safe.
    // Needed when the SQL text itself is built at runtime (e.g. choosing a column) - but never put USER TEXT in it.
    public Task<List<ProductModel>> SearchWithFromSqlRawAndParameter(string text)
    {
        var pattern = new SqliteParameter("@pattern", "%" + text + "%");
        return database.Products.FromSqlRaw("SELECT * FROM Products WHERE Name LIKE @pattern", pattern).AsNoTracking().ToListAsync();
    }

    // WRONG: FromSqlRaw with the text JOINED into the SQL. "' OR 1=1 --" becomes part of the SQL -> every row.
    // EF's analyzer warns (EF1002) when an INTERPOLATED string is passed to FromSqlRaw - but joining with + like
    // this produces NO warning at all. (Both checked while writing this.)
    public Task<List<ProductModel>> SearchWithStringConcatenation(string text)
    {
        string sql = "SELECT * FROM Products WHERE Name LIKE '%" + text + "%'";
        return database.Products.FromSqlRaw(sql).AsNoTracking().ToListAsync();
    }

    // Plain LINQ: no SQL written at all; EF makes "LIKE" with a parameter itself. The usual choice.
    public Task<List<ProductModel>> SearchWithLinq(string text)
    {
        return database.Products.Where(product => product.Name.Contains(text)).AsNoTracking().ToListAsync();
    }

    // ---------- bulk price raise ----------

    // Load every row, change it in C#, SaveChanges: one UPDATE PER ROW (plus the SELECT), but goes through the
    // change tracker (so concurrency tokens, SaveChanges overrides and events all apply). ExecuteUpdate skips those.
    public async Task<int> RaisePricesWithLoadAndSave(string category, int percent)
    {
        List<ProductModel> products = await database.Products.Where(product => product.Category == category && !product.IsDiscontinued).ToListAsync();
        foreach (ProductModel product in products)
        {
            product.PriceInCents = product.PriceInCents * (100 + percent) / 100;
            product.Version++;
        }
        await database.SaveChangesAsync();
        return products.Count;
    }

    // ---------- concurrency ----------

    // WRONG for shared data: "LAST WRITE WINS" - update without checking the version. The second person
    // silently overwrites the first person's change (a "lost update").
    public Task<int> UpdatePriceWithoutVersionCheck(int id, long newPriceInCents)
    {
        return database.Products.Where(product => product.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(product => product.PriceInCents, newPriceInCents));
    }

    // The version check done by hand in ONE statement: "UPDATE ... WHERE Id = @id AND Version = @seen".
    // 0 rows changed = someone else changed it first. Same protection as the concurrency token, without loading.
    public Task<int> UpdatePriceWithVersionInWhere(int id, long newPriceInCents, int versionTheUserSaw)
    {
        return database.Products.Where(product => product.Id == id && product.Version == versionTheUserSaw)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(product => product.PriceInCents, newPriceInCents)
                .SetProperty(product => product.Version, versionTheUserSaw + 1));
    }
}
