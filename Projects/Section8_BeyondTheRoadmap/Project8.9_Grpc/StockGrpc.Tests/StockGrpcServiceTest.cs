using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using StockGrpc.Protos;

namespace StockGrpc.Tests;

// PositivePositive - valid request -> reply
// PositiveNegative - well-formed request the system refuses (unknown SKU, not enough stock) -> NotFound / FailedPrecondition
// NegativeNegative - malformed request (bad SKU format, zero quantity) -> InvalidArgument
public class StockGrpcServiceTest
{
    // A NEW app per test (fresh stock), and a REAL gRPC client talking to it in memory.
    private static StockService.StockServiceClient CreateClient()
    {
        var factory = new WebApplicationFactory<Program>();
        GrpcChannel channel = GrpcChannel.ForAddress(factory.Server.BaseAddress, new GrpcChannelOptions
        {
            HttpHandler = factory.Server.CreateHandler(),
        });
        return new StockService.StockServiceClient(channel);
    }

    // ---------- GetStock (unary) ----------

    [Fact]
    public async Task GetStock_PositivePositive_KnownSku_ReturnsStock()
    {
        StockReply reply = await CreateClient().GetStockAsync(new GetStockRequest { Sku = "KB-01" });

        Assert.Equal("Keyboard", reply.Name);
        Assert.Equal(10, reply.Quantity);
    }

    [Fact]
    public async Task GetStock_PositiveNegative_WellFormedUnknownSku_ReturnsNotFound()
    {
        RpcException error = await Assert.ThrowsAsync<RpcException>(() => CreateClient().GetStockAsync(new GetStockRequest { Sku = "ZZ-99" }).ResponseAsync);

        Assert.Equal(StatusCode.NotFound, error.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("kb-01")]
    [InlineData("KEYBOARD")]
    public async Task GetStock_NegativeNegative_MalformedSku_ReturnsInvalidArgument(string sku)
    {
        RpcException error = await Assert.ThrowsAsync<RpcException>(() => CreateClient().GetStockAsync(new GetStockRequest { Sku = sku }).ResponseAsync);

        Assert.Equal(StatusCode.InvalidArgument, error.StatusCode);
    }

    // ---------- ReserveStock (unary) ----------

    [Fact]
    public async Task ReserveStock_PositivePositive_EnoughStock_ReservesAndLowersQuantity()
    {
        StockService.StockServiceClient client = CreateClient();

        ReserveStockReply reply = await client.ReserveStockAsync(new ReserveStockRequest { Sku = "MS-01", Quantity = 3 });

        Assert.Equal(0, reply.Remaining); // exactly all of it is allowed
        Assert.Equal(0, (await client.GetStockAsync(new GetStockRequest { Sku = "MS-01" })).Quantity);
    }

    [Fact]
    public async Task ReserveStock_PositiveNegative_MoreThanInStock_ReturnsFailedPreconditionAndChangesNothing()
    {
        StockService.StockServiceClient client = CreateClient();

        RpcException error = await Assert.ThrowsAsync<RpcException>(() => client.ReserveStockAsync(new ReserveStockRequest { Sku = "MS-01", Quantity = 4 }).ResponseAsync);

        Assert.Equal(StatusCode.FailedPrecondition, error.StatusCode);
        Assert.Contains("Only 3", error.Status.Detail);
        Assert.Equal(3, (await client.GetStockAsync(new GetStockRequest { Sku = "MS-01" })).Quantity);
    }

    [Theory]
    [InlineData("MS-01", 0)]
    [InlineData("MS-01", -2)]
    [InlineData("bad", 1)]
    public async Task ReserveStock_NegativeNegative_MalformedRequest_ReturnsInvalidArgument(string sku, int quantity)
    {
        RpcException error = await Assert.ThrowsAsync<RpcException>(() => CreateClient().ReserveStockAsync(new ReserveStockRequest { Sku = sku, Quantity = quantity }).ResponseAsync);

        Assert.Equal(StatusCode.InvalidArgument, error.StatusCode);
    }

    // ---------- WatchStock (server streaming) ----------

    [Fact]
    public async Task WatchStock_PositivePositive_SendsCurrentLevelThenEachChange()
    {
        StockService.StockServiceClient client = CreateClient();
        using var stopWatching = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using AsyncServerStreamingCall<StockReply> watch = client.WatchStock(new WatchStockRequest { Sku = "KB-01" }, cancellationToken: stopWatching.Token);
        IAsyncStreamReader<StockReply> updates = watch.ResponseStream;

        Assert.True(await updates.MoveNext(stopWatching.Token));
        Assert.Equal(10, updates.Current.Quantity);                      // the current level straight away

        await client.ReserveStockAsync(new ReserveStockRequest { Sku = "KB-01", Quantity = 4 });
        Assert.True(await updates.MoveNext(stopWatching.Token));
        Assert.Equal(6, updates.Current.Quantity);                       // pushed after the change

        await client.ReserveStockAsync(new ReserveStockRequest { Sku = "KB-01", Quantity = 1 });
        Assert.True(await updates.MoveNext(stopWatching.Token));
        Assert.Equal(5, updates.Current.Quantity);
    }

    [Fact]
    public async Task WatchStock_PositiveNegative_ChangesToOtherProducts_AreNotSent()
    {
        StockService.StockServiceClient client = CreateClient();
        using var stopWatching = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using AsyncServerStreamingCall<StockReply> watch = client.WatchStock(new WatchStockRequest { Sku = "KB-01" }, cancellationToken: stopWatching.Token);
        await watch.ResponseStream.MoveNext(stopWatching.Token); // the first, current level

        await client.ReserveStockAsync(new ReserveStockRequest { Sku = "MS-01", Quantity = 1 }); // a DIFFERENT product
        using var waitBriefly = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        Task<bool> next = watch.ResponseStream.MoveNext(waitBriefly.Token);

        RpcException stopped = await Assert.ThrowsAsync<RpcException>(() => next); // nothing came; we gave up waiting
        Assert.Equal(StatusCode.Cancelled, stopped.StatusCode);
    }

    [Fact]
    public async Task WatchStock_NegativeNegative_UnknownOrMalformedSku_EndsWithError()
    {
        StockService.StockServiceClient client = CreateClient();

        using AsyncServerStreamingCall<StockReply> unknown = client.WatchStock(new WatchStockRequest { Sku = "ZZ-99" });
        RpcException notFound = await Assert.ThrowsAsync<RpcException>(() => unknown.ResponseStream.MoveNext());

        using AsyncServerStreamingCall<StockReply> malformed = client.WatchStock(new WatchStockRequest { Sku = "nope" });
        RpcException invalid = await Assert.ThrowsAsync<RpcException>(() => malformed.ResponseStream.MoveNext());

        Assert.Equal(StatusCode.NotFound, notFound.StatusCode);
        Assert.Equal(StatusCode.InvalidArgument, invalid.StatusCode);
    }
}
