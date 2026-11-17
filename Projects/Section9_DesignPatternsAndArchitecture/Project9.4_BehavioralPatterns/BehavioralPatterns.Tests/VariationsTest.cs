using BehavioralPatterns.Commands;
using BehavioralPatterns.Contexts;
using BehavioralPatterns.Events;
using BehavioralPatterns.Models;
using BehavioralPatterns.Observers;
using BehavioralPatterns.Publishers;
using BehavioralPatterns.Services;
using BehavioralPatterns.Strategies;
using BehavioralPatterns.Variations;

namespace BehavioralPatterns.Tests;

// Each pattern's original vs a simpler variation (BehavioralPatterns/Variations/).
public class VariationsTest
{
    private readonly ShippingService strategies = new ShippingService(
        [new StandardShippingStrategy(), new ExpressShippingStrategy(), new FreeOverThresholdShippingStrategy()]);

    [Theory]
    [InlineData("standard", 60, 2)]
    [InlineData("express", 60, 2)]
    [InlineData("free-over-100", 120, 3)]
    [InlineData("free-over-100", 60, 3)]
    public void Strategy_PositivePositive_FuncDictionaryAndSwitch_PriceLikeTheClasses(string option, decimal total, decimal weight)
    {
        decimal expected = strategies.Quote(option, total, weight);

        Assert.Equal(expected, ShippingPriceVariation.WithFuncDictionary(option, total, weight));
        Assert.Equal(expected, ShippingPriceVariation.WithSwitch(option, total, weight));
    }

    [Fact]
    public void State_PositivePositive_EnumTable_FollowsTheSamePathAsTheStateClasses()
    {
        var order = new OrderContext();
        order.Pay();
        order.Ship();
        order.Deliver();

        OrderStatusVariation status = OrderStatusVariation.Pending;
        foreach (string action in new[] { "pay", "ship", "deliver" })
        {
            status = OrderStatusTransitionVariation.Apply(status, action);
        }

        Assert.Equal("delivered", order.State.Name);
        Assert.Equal(OrderStatusVariation.Delivered, status);
    }

    [Fact]
    public void State_PositiveNegative_ForbiddenMove_SameMessageBothWays()
    {
        var order = new OrderContext();
        order.Pay();
        order.Ship();

        string fromClasses = Assert.Throws<InvalidOperationException>(() => order.Cancel()).Message;
        string fromTable = Assert.Throws<InvalidOperationException>(() => OrderStatusTransitionVariation.Apply(OrderStatusVariation.Shipped, "cancel")).Message;

        Assert.Equal(fromClasses, fromTable);
    }

    [Fact]
    public void Undo_PositivePositive_SnapshotsGiveTheSameTextAsCommands_ButStoreMore()
    {
        var document = new TextDocumentModel();
        var history = new CommandHistoryService();
        history.Execute(new InsertTextCommand(document, 0, "Hello"));
        history.Execute(new InsertTextCommand(document, 5, " world"));
        history.Undo();

        var snapshots = new SnapshotEditorVariation();
        snapshots.Change(text => text + "Hello");
        snapshots.Change(text => text + " world");
        snapshots.Undo();

        Assert.Equal(document.ToString(), snapshots.Text);   // "Hello" both ways
        Assert.Equal("".Length + "Hello world".Length, snapshots.SavedCharacters); // whole copies kept: "" (undo) + "Hello world" (redo)
    }

    [Fact]
    public void Observer_PositiveNegative_OneHandlerFails_EventStopsThere_PublisherTellsEveryone()
    {
        var plainEvent = new OrderPlacedEventVariation();
        int laterHandlerCalls = 0;
        plainEvent.OrderPlaced += _ => throw new InvalidOperationException("email server down");
        plainEvent.OrderPlaced += _ => laterHandlerCalls++;

        var publisher = new OrderEventPublisher();
        var email = new EmailOrderPlacedObserver();
        using IDisposable failing = publisher.Subscribe(new FailingObserver());
        using IDisposable working = publisher.Subscribe(email);

        Assert.Throws<InvalidOperationException>(() => plainEvent.Publish(new OrderPlacedEvent(1, "Ann", 10m)));
        Assert.Throws<AggregateException>(() => publisher.Publish(new OrderPlacedEvent(1, "Ann", 10m)));

        Assert.Equal(0, laterHandlerCalls);   // the event never reached the second handler
        Assert.Single(email.SentEmails);      // the publisher still told the working observer
    }

    [Fact]
    public void Variations_NegativeNegative_UnknownOptionOrNullEdit_Rejected()
    {
        Assert.Throws<KeyNotFoundException>(() => ShippingPriceVariation.WithFuncDictionary("teleport", 1m, 1m));
        Assert.Throws<KeyNotFoundException>(() => ShippingPriceVariation.WithSwitch("teleport", 1m, 1m));
        Assert.Throws<ArgumentNullException>(() => new SnapshotEditorVariation().Change(null!));
        Assert.False(new SnapshotEditorVariation().Undo());
    }

    private class FailingObserver : IOrderPlacedObserver
    {
        public void OnOrderPlaced(OrderPlacedEvent placedEvent)
        {
            throw new InvalidOperationException("email server down");
        }
    }
}
