using BehavioralPatterns.Handlers;
using BehavioralPatterns.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace BehavioralPatterns.Mediators;

// Finds the handler for a request in the DI container: request type PlaceOrderRequest
// -> service type IRequestHandler<PlaceOrderRequest, PlaceOrderResult>.
public class ServiceProviderMediator : IMediator
{
    private readonly IServiceProvider services;

    public ServiceProviderMediator(IServiceProvider services)
    {
        this.services = services;
    }

    public TResult Send<TResult>(IRequest<TResult> request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Type handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResult));
        object handler = services.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for {request.GetType().Name}.");
        // "dynamic" lets us call Handle without knowing the exact handler type at compile time.
        return ((dynamic)handler).Handle((dynamic)request);
    }
}
