using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using StockGrpc.Protos;
using StockGrpc.Variations;

namespace StockGrpc.Tests;

// Client call styles (StockGrpc/Variations/StockClientVariation.cs) against the real service in memory.
public class VariationsTest
{
    private static StockService.StockServiceClient CreateClient()
    {
        var factory = new WebApplicationFactory<Program>();
        GrpcChannel channel = GrpcChannel.ForAddress(factory.Server.BaseAddress, new GrpcChannelOptions { HttpHandler = factory.Server.CreateHandler() });
        return new StockService.StockServiceClient(channel);
    }

    [Fact]
    public async Task Calls_PositivePositive_BlockingDeadlineAndHeaders_GetTheSameReplyAsAsync()
    {
        StockService.StockServiceClient client = CreateClient();
        StockReply original = await client.GetStockAsync(new GetStockRequest { Sku = "KB-01" });

        Assert.Equal(original, StockClientVariation.GetBlocking(client, "KB-01"));
        Assert.Equal(original, await StockClientVariation.GetWithDeadline(client, "KB-01", TimeSpan.FromSeconds(5)));
        Assert.Equal(original, (await StockClientVariation.GetWithHeaders(client, "KB-01")).Reply);
    }

    [Fact]
    public async Task Streams_PositivePositive_MoveNextAndAwaitForeach_ReadTheSameMessages()
    {
        StockService.StockServiceClient client = CreateClient();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        Task<List<int>> viaMoveNext = StockClientVariation.WatchWithMoveNext(client, "MS-01", 2, timeout.Token);
        Task<List<int>> viaForeach = StockClientVariation.WatchWithAwaitForeach(client, "MS-01", 2, timeout.Token);
        await Task.Delay(300);                                                                  // both are now watching
        await client.ReserveStockAsync(new ReserveStockRequest { Sku = "MS-01", Quantity = 1 }); // stock 3 -> 2

        Assert.Equal([3, 2], await viaMoveNext);
        Assert.Equal([3, 2], await viaForeach);
    }

    [Fact]
    public async Task Deadline_PositiveNegative_StreamThatNeverEnds_StoppedWithDeadlineExceeded()
    {
        StockService.StockServiceClient client = CreateClient();
        using AsyncServerStreamingCall<StockReply> call = client.WatchStock(new WatchStockRequest { Sku = "KB-01" }, deadline: DateTime.UtcNow.AddMilliseconds(300));

        var error = await Assert.ThrowsAsync<RpcException>(async () =>
        {
            await foreach (StockReply _ in call.ResponseStream.ReadAllAsync())
            {
            }
        });

        Assert.Equal(StatusCode.DeadlineExceeded, error.StatusCode); // no deadline = the loop would wait forever
    }

    [Fact]
    public async Task Calls_NegativeNegative_BadSku_SameStatusCodeWhicheverStyle()
    {
        StockService.StockServiceClient client = CreateClient();

        Assert.Equal(StatusCode.InvalidArgument, Assert.Throws<RpcException>(() => StockClientVariation.GetBlocking(client, "bad")).StatusCode);
        Assert.Equal(StatusCode.InvalidArgument, (await Assert.ThrowsAsync<RpcException>(() => StockClientVariation.GetWithDeadline(client, "bad", TimeSpan.FromSeconds(5)))).StatusCode);
    }
}
