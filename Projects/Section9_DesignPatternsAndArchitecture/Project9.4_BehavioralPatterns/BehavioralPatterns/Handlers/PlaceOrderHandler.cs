using BehavioralPatterns.Events;
using BehavioralPatterns.Publishers;
using BehavioralPatterns.Requests;
using BehavioralPatterns.Results;
using BehavioralPatterns.Services;

namespace BehavioralPatterns.Handlers;

// All the work for "place an order" in one place: the Strategy picks the shipping price,
// then the Observer publisher tells everyone who cares.
public class PlaceOrderHandler : IRequestHandler<PlaceOrderRequest, PlaceOrderResult>
{
    private static int lastOrderId;
    private readonly ShippingService shipping;
    private readonly OrderEventPublisher publisher;

    public PlaceOrderHandler(ShippingService shipping, OrderEventPublisher publisher)
    {
        this.shipping = shipping;
        this.publisher = publisher;
    }

    public PlaceOrderResult Handle(PlaceOrderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Customer) || request.Total <= 0)
        {
            return new PlaceOrderResult(false, 0, 0, 0, "Customer and a total above 0 are required.");
        }
        decimal shippingCost;
        try
        {
            shippingCost = shipping.Quote(request.ShippingOption, request.Total, request.WeightKilograms);
        }
        catch (Exception exception) when (exception is KeyNotFoundException or ArgumentException)
        {
            return new PlaceOrderResult(false, 0, 0, 0, exception.Message);
        }
        int orderId = Interlocked.Increment(ref lastOrderId);
        decimal grandTotal = request.Total + shippingCost;
        publisher.Publish(new OrderPlacedEvent(orderId, request.Customer, grandTotal));
        return new PlaceOrderResult(true, orderId, shippingCost, grandTotal, null);
    }
}
