using EfCoreModelling.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EfCoreModelling.Tests.Helpers;

// A fresh in-memory database with the demo data, for any of the DbContexts. Every SQL command is kept in Sql,
// so tests can check WHAT EF sent to the database.
public sealed class ShopDatabaseHelper<TContext> : IDisposable where TContext : DbContext
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly Func<DbContextOptions<TContext>, TContext> create;

    public ShopDatabaseHelper(Func<DbContextOptions<TContext>, TContext> create)
    {
        this.create = create;
        connection.Open();
        using TContext setup = NewContext();
        setup.Database.EnsureCreated();
        (AcmeInvoiceId, CementId) = ShopSeeder.Seed(setup);
        Sql.Clear();
    }

    public List<string> Sql { get; } = [];

    public Guid AcmeInvoiceId { get; }

    public Guid CementId { get; }

    public TContext NewContext()
    {
        DbContextOptions<TContext> options = new DbContextOptionsBuilder<TContext>()
            .UseSqlite(connection)
            .LogTo(message => Sql.Add(message), [DbLoggerCategory.Database.Command.Name], LogLevel.Information)
            .Options;
        return create(options);
    }

    public List<string> Query(string sql)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        using SqliteDataReader reader = command.ExecuteReader();
        var rows = new List<string>();
        while (reader.Read())
        {
            rows.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(index => reader.IsDBNull(index) ? "NULL" : reader.GetValue(index).ToString())));
        }
        return rows;
    }

    public void Dispose()
    {
        connection.Dispose();
    }
}
