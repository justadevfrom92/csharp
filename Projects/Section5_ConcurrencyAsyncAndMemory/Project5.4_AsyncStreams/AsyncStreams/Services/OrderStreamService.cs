using AsyncStreams.DbContexts;
using AsyncStreams.Models;
using Microsoft.EntityFrameworkCore;

namespace AsyncStreams.Services;

// Reading database rows as an ASYNC STREAM: rows arrive one at a time while the query runs,
// instead of ToListAsync() building the whole list in memory first.
public class OrderStreamService
{
    private readonly ShopDbContext database;

    public OrderStreamService(ShopDbContext database)
    {
        this.database = database;
    }

    // AsAsyncEnumerable(): EF reads one row, hands it out, reads the next. Nothing runs until someone loops.
    public IAsyncEnumerable<OrderModel> ReadBigOrders(decimal minimumTotal)
    {
        return database.Orders
            .Where(order => order.Total >= minimumTotal)
            .OrderBy(order => order.Id)
            .AsAsyncEnumerable();
    }

    // Consuming the stream with await foreach - only one order is in memory at a time.
    public async Task<decimal> SumBigOrdersAsync(decimal minimumTotal, CancellationToken cancellationToken)
    {
        decimal sum = 0;
        await foreach (OrderModel order in ReadBigOrders(minimumTotal).WithCancellation(cancellationToken))
        {
            sum += order.Total;
        }
        return sum;
    }

    // .NET 10: LINQ for async streams is built in (System.Linq.AsyncEnumerable). Where/Select/Take run on
    // the stream in C# memory - unlike the Where above, which EF turned into SQL.
    public async Task<List<string>> FirstCustomersAsync(int count, CancellationToken cancellationToken)
    {
        return await ReadBigOrders(0m)
            .Where(order => order.Customer.Length <= 3)
            .Select(order => order.Customer)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
