using EfCoreAdvancedQueries.DbContexts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EfCoreAdvancedQueries.Tests.Helpers;

// Each test gets its OWN brand-new database that lives only in memory.
// Why SQLite in-memory and not EF's "InMemory" provider? The InMemory provider is not a real database:
// it can't run FromSql/ExecuteSql at all, and it ignores concurrency tokens. SQLite in-memory runs the
// REAL SQL, so the tests prove the code works against a real database - and it's still fast.
// The in-memory database exists only while its connection is open, so the connection is kept open
// until the test disposes this helper.
public class TestDatabaseHelper : IDisposable
{
    private readonly SqliteConnection connection;

    public TestDatabaseHelper()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using ShopDbContext setup = CreateContext();
        setup.Database.EnsureCreated(); // creates the table and the 6 seed products
    }

    // A NEW DbContext each call, all sharing the same database. Two contexts = two separate users.
    public ShopDbContext CreateContext()
    {
        DbContextOptions<ShopDbContext> options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseSqlite(connection)
            .Options;
        return new ShopDbContext(options);
    }

    public void Dispose()
    {
        connection.Dispose();
    }
}
