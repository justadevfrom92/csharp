using Grpc.Core;
using StockGrpc.Protos;

namespace StockGrpc.Variations;

// Other ways to CALL the stock service with the generated client. The tests (original) use
// "await client.GetStockAsync(...)" and "await foreach (... ReadAllAsync())".
public static class StockClientVariation
{
    // BLOCKING call: the generated client also has GetStock (no "Async"). Same reply, but the calling thread
    // waits doing nothing. Fine in a console tool; in a web app it wastes a thread per call.
    public static StockReply GetBlocking(StockService.StockServiceClient client, string sku)
    {
        return client.GetStock(new GetStockRequest { Sku = sku });
    }

    // DEADLINE: "give up after this long". The server is told too (context.CancellationToken is cancelled there),
    // so it can stop working. Without one, a stuck call can wait forever.
    public static Task<StockReply> GetWithDeadline(StockService.StockServiceClient client, string sku, TimeSpan timeLimit)
    {
        return client.GetStockAsync(new GetStockRequest { Sku = sku }, deadline: DateTime.UtcNow.Add(timeLimit)).ResponseAsync;
    }

    // METADATA (gRPC's headers): extra values next to the message, e.g. a correlation id or an auth token.
    // Returns the response headers the server sent back.
    public static async Task<(StockReply Reply, Metadata ResponseHeaders)> GetWithHeaders(StockService.StockServiceClient client, string sku)
    {
        var headers = new Metadata { { "x-correlation-id", "demo-123" } };
        using AsyncUnaryCall<StockReply> call = client.GetStockAsync(new GetStockRequest { Sku = sku }, headers);
        Metadata responseHeaders = await call.ResponseHeadersAsync;
        return (await call.ResponseAsync, responseHeaders);
    }

    // Reading a server STREAM with MoveNext/Current (the older style) instead of "await foreach ReadAllAsync()".
    // Same messages; await foreach is shorter and harder to get wrong.
    public static async Task<List<int>> WatchWithMoveNext(StockService.StockServiceClient client, string sku, int howMany, CancellationToken cancellationToken)
    {
        using AsyncServerStreamingCall<StockReply> call = client.WatchStock(new WatchStockRequest { Sku = sku }, cancellationToken: cancellationToken);
        var quantities = new List<int>();
        while (quantities.Count < howMany && await call.ResponseStream.MoveNext(cancellationToken))
        {
            quantities.Add(call.ResponseStream.Current.Quantity);
        }
        return quantities;
    }

    // The same, with await foreach.
    public static async Task<List<int>> WatchWithAwaitForeach(StockService.StockServiceClient client, string sku, int howMany, CancellationToken cancellationToken)
    {
        using AsyncServerStreamingCall<StockReply> call = client.WatchStock(new WatchStockRequest { Sku = sku }, cancellationToken: cancellationToken);
        var quantities = new List<int>();
        await foreach (StockReply reply in call.ResponseStream.ReadAllAsync(cancellationToken))
        {
            quantities.Add(reply.Quantity);
            if (quantities.Count == howMany)
            {
                break;
            }
        }
        return quantities;
    }
}
