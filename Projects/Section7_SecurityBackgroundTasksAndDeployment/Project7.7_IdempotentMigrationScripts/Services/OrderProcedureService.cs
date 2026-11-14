using System.Data;
using IdempotentMigrationScripts.DbContexts;
using IdempotentMigrationScripts.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace IdempotentMigrationScripts.Services;

// Calls the three stored procedures created by the AddOrderStoredProcedures migration.
// Every {value} in FromSql / ExecuteSql becomes a SQL PARAMETER, never pasted into the SQL text,
// so calling a stored procedure this way is safe from SQL injection.
public class OrderProcedureService
{
    private readonly OrdersDbContext database;

    public OrderProcedureService(OrdersDbContext database)
    {
        this.database = database;
    }

    // 1. A procedure that RETURNS ROWS -> FromSql on the DbSet turns each row into an OrderModel.
    //    You can't add LINQ (Where, OrderBy) after EXEC: SQL Server can't use a procedure inside a bigger query.
    //    So the sorting is done inside the procedure.
    public Task<List<OrderModel>> GetOrdersForCustomerAsync(int customerId)
    {
        CheckCustomerId(customerId);
        return database.Orders
            .FromSql($"EXEC GetOrdersForCustomer @CustomerId = {customerId}")
            .AsNoTracking()
            .ToListAsync();
    }

    // 2. A procedure that CHANGES DATA and returns no rows -> ExecuteSql. Returns how many rows changed.
    public Task<int> ApplyCustomerDiscountAsync(int customerId, int percent)
    {
        CheckCustomerId(customerId);
        if (percent < 1 || percent > 50)
        {
            // Checked here too, so a bad value never reaches the database. The procedure checks again
            // (THROW 50001) because other programs may call it directly.
            throw new ArgumentOutOfRangeException(nameof(percent), "Discount must be 1 to 50 percent.");
        }
        return database.Database.ExecuteSqlAsync(
            $"EXEC ApplyCustomerDiscount @CustomerId = {customerId}, @Percent = {percent}");
    }

    // 3. A procedure with an OUTPUT parameter -> pass a SqlParameter with Direction = Output,
    //    run the procedure, then read the value the procedure put into it.
    public async Task<decimal> GetCustomerOrderTotalAsync(int customerId)
    {
        CheckCustomerId(customerId);
        var totalParameter = new SqlParameter("@Total", SqlDbType.Decimal)
        {
            Direction = ParameterDirection.Output,
            Precision = 18,
            Scale = 2,
        };
        await database.Database.ExecuteSqlAsync(
            $"EXEC GetCustomerOrderTotal @CustomerId = {customerId}, @Total = {totalParameter} OUTPUT");
        return (decimal)totalParameter.Value;
    }

    private static void CheckCustomerId(int customerId)
    {
        if (customerId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(customerId), "Customer id must be more than 0.");
        }
    }
}
