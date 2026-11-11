using System.Text.RegularExpressions;
using System.Threading.Channels;
using Grpc.Core;
using StockGrpc.Protos;
using StockGrpc.Stores;

namespace StockGrpc.Services;

// Implements the service from stock.proto. StockService.StockServiceBase was GENERATED from the .proto file;
// we override its methods. Errors are reported with gRPC STATUS CODES (RpcException), not HTTP status codes.
public class StockGrpcService : StockService.StockServiceBase
{
    private readonly StockStore store;

    public StockGrpcService(StockStore store)
    {
        this.store = store;
    }

    public override Task<StockReply> GetStock(GetStockRequest request, ServerCallContext context)
    {
        CheckSku(request.Sku);
        return Task.FromResult(CurrentStock(request.Sku));
    }

    public override Task<ReserveStockReply> ReserveStock(ReserveStockRequest request, ServerCallContext context)
    {
        CheckSku(request.Sku);
        if (request.Quantity <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Quantity must be more than 0."));
        }
        StockReply current = CurrentStock(request.Sku); // throws NotFound for an unknown SKU

        int? remaining = store.TryReserve(request.Sku, request.Quantity);
        if (remaining == null)
        {
            // The request is fine, but the system's state doesn't allow it: FailedPrecondition.
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"Only {current.Quantity} of {request.Sku} left."));
        }
        return Task.FromResult(new ReserveStockReply { Sku = request.Sku, Reserved = request.Quantity, Remaining = remaining.Value });
    }

    // SERVER STREAMING: send the current level now, then a new message every time it changes,
    // until the client cancels (or disconnects).
    public override async Task WatchStock(WatchStockRequest request, IServerStreamWriter<StockReply> responseStream, ServerCallContext context)
    {
        CheckSku(request.Sku);
        await responseStream.WriteAsync(CurrentStock(request.Sku));

        // StockChanged fires on other threads; a Channel hands the changes over to this loop safely.
        Channel<string> changes = Channel.CreateUnbounded<string>();
        void OnChanged(string changedSku)
        {
            if (changedSku == request.Sku)
            {
                changes.Writer.TryWrite(changedSku);
            }
        }

        store.StockChanged += OnChanged;
        try
        {
            await foreach (string _ in changes.Reader.ReadAllAsync(context.CancellationToken))
            {
                await responseStream.WriteAsync(CurrentStock(request.Sku));
            }
        }
        catch (OperationCanceledException)
        {
            // The client stopped watching - a normal end.
        }
        finally
        {
            store.StockChanged -= OnChanged; // always unsubscribe, or every finished stream would leak
        }
    }

    private StockReply CurrentStock(string sku)
    {
        var item = store.Find(sku);
        if (item == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"No product with SKU '{sku}'."));
        }
        return new StockReply { Sku = sku, Name = item.Value.Name, Quantity = item.Value.Quantity };
    }

    private static void CheckSku(string sku)
    {
        if (!Regex.IsMatch(sku ?? "", "^[A-Z]{2}-[0-9]{2}$"))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "SKU must look like 'KB-01'."));
        }
    }
}
