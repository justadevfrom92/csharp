using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IdempotentMigrationScripts.Data;

// Used ONLY by the "dotnet ef" tool. Generating a script never connects to this database.
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext>
{
    public OrdersDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseSqlServer("Server=localhost;Database=Orders;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new OrdersDbContext(options);
    }
}
