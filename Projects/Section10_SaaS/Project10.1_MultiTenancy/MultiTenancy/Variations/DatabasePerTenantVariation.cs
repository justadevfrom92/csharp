using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MultiTenancy.Variations;

// The other way to separate tenants: each tenant gets ITS OWN DATABASE. No TenantId filter is needed -
// another tenant's rows simply aren't there. Safer and easy to delete/export one customer,
// but N databases to migrate, back up and pay for. (A middle option: one schema per tenant.)
public class DatabasePerTenantVariation : IDisposable
{
    // In-memory SQLite: a database lives while a connection to it is open, so one is kept per tenant.
    private readonly Dictionary<string, SqliteConnection> connections = new();

    public UnfilteredDbContextVariation CreateFor(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        if (!connections.TryGetValue(tenantId, out SqliteConnection? connection))
        {
            connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            connections[tenantId] = connection;
            using UnfilteredDbContextVariation setup = Create(connection);
            setup.Database.EnsureCreated();
            setup.Projects.RemoveRange(setup.Projects); // start empty: the seed rows belong to the shared demo
            setup.SaveChanges();
        }
        return Create(connection);
    }

    public void Dispose()
    {
        foreach (SqliteConnection connection in connections.Values)
        {
            connection.Dispose();
        }
    }

    private static UnfilteredDbContextVariation Create(SqliteConnection connection)
    {
        return new UnfilteredDbContextVariation(new DbContextOptionsBuilder<UnfilteredDbContextVariation>().UseSqlite(connection).Options);
    }
}
