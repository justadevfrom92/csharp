using AsyncStreams.DbContexts;
using AsyncStreams.Models;
using AsyncStreams.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AsyncStreams.Tests;

public class OrderStreamServiceTest : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly ShopDbContext database;
    private readonly OrderStreamService service;

    public OrderStreamServiceTest()
    {
        connection.Open();
        database = new ShopDbContext(new DbContextOptionsBuilder<ShopDbContext>().UseSqlite(connection).Options);
        database.Database.EnsureCreated();
        service = new OrderStreamService(database);
    }

    public void Dispose()
    {
        database.Dispose();
        connection.Dispose();
    }

    [Fact]
    public async Task Orders_PositivePositive_StreamsSumsAndUsesAsyncLinq()
    {
        var ids = new List<int>();
        await foreach (OrderModel order in service.ReadBigOrders(100m))
        {
            ids.Add(order.Id);
        }

        Assert.Equal([2, 3, 5], ids);
        Assert.Equal(1419m, await service.SumBigOrdersAsync(100m, CancellationToken.None));
        Assert.Equal(["Ann", "Bob"], await service.FirstCustomersAsync(2, CancellationToken.None));
    }

    [Fact]
    public async Task Orders_PositiveNegative_NothingBigEnough_EmptyAndZero()
    {
        Assert.Empty(await service.ReadBigOrders(10_000m).ToListAsync());
        Assert.Equal(0m, await service.SumBigOrdersAsync(10_000m, CancellationToken.None));
        Assert.Empty(await service.FirstCustomersAsync(0, CancellationToken.None));
    }

    [Fact]
    public async Task Orders_NegativeNegative_CancelledToken_Throws()
    {
        var cancelled = new CancellationToken(canceled: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.SumBigOrdersAsync(100m, cancelled));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.FirstCustomersAsync(2, cancelled));
    }
}
