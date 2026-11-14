using BehavioralPatterns.Events;
using BehavioralPatterns.Handlers;
using BehavioralPatterns.Mediators;
using BehavioralPatterns.Observers;
using BehavioralPatterns.Publishers;
using BehavioralPatterns.Requests;
using BehavioralPatterns.Results;
using BehavioralPatterns.Services;
using BehavioralPatterns.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace BehavioralPatterns.Tests;

public class MediatorAndObserverTest : IDisposable
{
    private readonly ServiceProvider services;
    private readonly IMediator mediator;
    private readonly OrderEventPublisher publisher;

    public MediatorAndObserverTest()
    {
        var collection = new ServiceCollection();
        collection.AddSingleton<IShippingCostStrategy, StandardShippingStrategy>();
        collection.AddSingleton<ShippingService>();
        collection.AddSingleton<OrderEventPublisher>();
        collection.AddTransient<IRequestHandler<PlaceOrderRequest, PlaceOrderResult>, PlaceOrderHandler>();
        collection.AddSingleton<IMediator, ServiceProviderMediator>();
        services = collection.BuildServiceProvider();
        mediator = services.GetRequiredService<IMediator>();
        publisher = services.GetRequiredService<OrderEventPublisher>();
    }

    public void Dispose()
    {
        services.Dispose();
    }

    private record UnhandledRequest : IRequest<int>;

    private class FailingObserver : IOrderPlacedObserver
    {
        public void OnOrderPlaced(OrderPlacedEvent placedEvent)
        {
            throw new InvalidOperationException("email server down");
        }
    }

    // ---------- Mediator ----------

    [Fact]
    public void Send_PositivePositive_RequestReachesItsHandler_AndObserversAreTold()
    {
        var email = new EmailOrderPlacedObserver();
        using IDisposable subscription = publisher.Subscribe(email);

        PlaceOrderResult result = mediator.Send(new PlaceOrderRequest("Ann", 50m, "standard", 1m));

        Assert.True(result.Succeeded);
        Assert.Equal(5.99m, result.Shipping);
        Assert.Equal(55.99m, result.GrandTotal);
        Assert.Single(email.SentEmails);
    }

    [Fact]
    public void Send_PositiveNegative_RequestBreaksARule_FailsAndNobodyIsTold()
    {
        var email = new EmailOrderPlacedObserver();
        using IDisposable subscription = publisher.Subscribe(email);

        PlaceOrderResult badOption = mediator.Send(new PlaceOrderRequest("Ann", 50m, "teleport", 1m));
        PlaceOrderResult zeroTotal = mediator.Send(new PlaceOrderRequest("Ann", 0m, "standard", 1m));

        Assert.False(badOption.Succeeded);
        Assert.Contains("teleport", badOption.Error);
        Assert.False(zeroTotal.Succeeded);
        Assert.Empty(email.SentEmails);
    }

    [Fact]
    public void Send_NegativeNegative_NullOrNoHandler_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => mediator.Send<PlaceOrderResult>(null!));
        Assert.Throws<InvalidOperationException>(() => mediator.Send(new UnhandledRequest()));
    }

    // ---------- Observer ----------

    [Fact]
    public void Publish_PositivePositive_EveryObserverIsTold_UntilItUnsubscribes()
    {
        var email = new EmailOrderPlacedObserver();
        var loyalty = new LoyaltyPointsOrderPlacedObserver();
        using IDisposable emailSubscription = publisher.Subscribe(email);
        IDisposable loyaltySubscription = publisher.Subscribe(loyalty);

        publisher.Publish(new OrderPlacedEvent(1, "Ann", 120.75m));
        loyaltySubscription.Dispose();
        publisher.Publish(new OrderPlacedEvent(2, "Ann", 30m));

        Assert.Equal(2, email.SentEmails.Count);
        Assert.Equal(120, loyalty.PointsByCustomer["Ann"]);
        Assert.Equal(1, publisher.ObserverCount);
    }

    [Fact]
    public void Publish_PositiveNegative_OneObserverFails_OthersStillTold_ThenErrorReported()
    {
        var email = new EmailOrderPlacedObserver();
        using IDisposable failing = publisher.Subscribe(new FailingObserver());
        using IDisposable working = publisher.Subscribe(email);
        using IDisposable twice = publisher.Subscribe(email); // subscribing twice doesn't double-notify

        var error = Assert.Throws<AggregateException>(() => publisher.Publish(new OrderPlacedEvent(1, "Ann", 10m)));

        Assert.Single(email.SentEmails);
        Assert.Contains("email server down", error.InnerExceptions[0].Message);
    }

    [Fact]
    public void Publish_NegativeNegative_NullEventOrObserver_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => publisher.Publish(null!));
        Assert.Throws<ArgumentNullException>(() => publisher.Subscribe(null!));
    }
}
