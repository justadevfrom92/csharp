using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Reactive.Testing;
using ReactiveExtensions.Models;
using ReactiveExtensions.Monitors;

namespace ReactiveExtensions.Tests;

// PositivePositive = valid input -> works; PositiveNegative = well-formed input that breaks a rule -> nothing / error passed on;
// NegativeNegative = broken or missing input -> rejected.
//
// Time-based tests use TestScheduler: a FAKE clock. scheduler.AdvanceBy(...) moves time forward instantly,
// so a test about "100 ms of quiet" runs in microseconds and gives the same result every time.
public class PriceMonitorTest
{
    private static PriceTickModel Tick(string symbol, decimal price)
    {
        return new PriceTickModel(symbol, price);
    }

    // ---------- WatchForJumps ----------

    [Fact]
    public void Jumps_PositivePositive_BigMovesPerSymbol_AreReported()
    {
        var ticks = new Subject<PriceTickModel>();
        var jumps = new List<PriceJumpModel>();
        using IDisposable subscription = PriceMonitor.WatchForJumps(ticks, 5m).Subscribe(jumps.Add);

        ticks.OnNext(Tick("MSFT", 400m));
        ticks.OnNext(Tick("AAPL", 200m));   // different symbol: not compared with MSFT
        ticks.OnNext(Tick("MSFT", 420m));   // +5.00% -> jump
        ticks.OnNext(Tick("AAPL", 180m));   // -10.00% -> jump

        Assert.Equal([new PriceJumpModel("MSFT", 400m, 420m, 5.00m), new PriceJumpModel("AAPL", 200m, 180m, -10.00m)], jumps);
    }

    [Fact]
    public void Jumps_PositiveNegative_SmallMovesOrSingleTick_ReportNothing()
    {
        var ticks = new Subject<PriceTickModel>();
        var jumps = new List<PriceJumpModel>();
        using IDisposable subscription = PriceMonitor.WatchForJumps(ticks, 5m).Subscribe(jumps.Add);

        ticks.OnNext(Tick("MSFT", 400m));
        ticks.OnNext(Tick("MSFT", 419.96m)); // +4.99%
        ticks.OnNext(Tick("AAPL", 200m));     // only one AAPL tick
        ticks.OnCompleted();

        Assert.Empty(jumps);
    }

    [Fact]
    public void Jumps_NegativeNegative_NullStreamOrBadPercent_Throws_AndErrorsPassThrough()
    {
        Assert.Throws<ArgumentNullException>(() => PriceMonitor.WatchForJumps(null!, 5m));
        Assert.Throws<ArgumentOutOfRangeException>(() => PriceMonitor.WatchForJumps(Observable.Empty<PriceTickModel>(), 0m));

        Exception? received = null;
        var ticks = new Subject<PriceTickModel>();
        using IDisposable subscription = PriceMonitor.WatchForJumps(ticks, 5m).Subscribe(_ => { }, error => received = error);
        ticks.OnError(new TimeoutException("feed lost"));

        Assert.IsType<TimeoutException>(received); // an error in the source reaches the subscriber's OnError
    }

    // ---------- MovingAverage ----------

    [Fact]
    public void Average_PositivePositive_SlidingWindow()
    {
        var averages = new List<decimal>();
        using IDisposable subscription = PriceMonitor.MovingAverage(new[] { 10m, 20m, 30m, 40m }.Select(price => Tick("X", price)).ToObservable(), 3)
            .Subscribe(averages.Add);

        Assert.Equal([20m, 30m], averages); // (10+20+30)/3, (20+30+40)/3
    }

    [Fact]
    public void Average_PositiveNegative_FewerTicksThanWindow_GivesNothing()
    {
        var averages = new List<decimal>();
        using IDisposable subscription = PriceMonitor.MovingAverage(new[] { Tick("X", 10m), Tick("X", 20m) }.ToObservable(), 3).Subscribe(averages.Add);

        Assert.Empty(averages);
    }

    [Fact]
    public void Average_NegativeNegative_NullOrZeroWindow_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => PriceMonitor.MovingAverage(null!, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => PriceMonitor.MovingAverage(Observable.Empty<PriceTickModel>(), 0));
    }

    // ---------- SettledPrices (Throttle) on a fake clock ----------

    [Fact]
    public void Throttle_PositivePositive_BurstBecomesOneTick_AfterTheQuietPeriod()
    {
        var scheduler = new TestScheduler();
        var ticks = new Subject<PriceTickModel>();
        var settled = new List<(long Time, decimal Price)>();
        using IDisposable subscription = PriceMonitor.SettledPrices(ticks, TimeSpan.FromMilliseconds(100), scheduler)
            .Subscribe(tick => settled.Add((scheduler.Clock, tick.Price)));

        ticks.OnNext(Tick("X", 1m));
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(50).Ticks);
        ticks.OnNext(Tick("X", 2m));
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(50).Ticks);
        ticks.OnNext(Tick("X", 3m));                                 // burst: 3 ticks, never 100 ms apart
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(100).Ticks);   // now quiet for 100 ms

        Assert.Equal([(TimeSpan.FromMilliseconds(200).Ticks, 3m)], settled); // only the LAST one, at 100 + 100 ms
    }

    [Fact]
    public void Throttle_PositiveNegative_NeverQuietLongEnough_NothingYet()
    {
        var scheduler = new TestScheduler();
        var ticks = new Subject<PriceTickModel>();
        var settled = new List<PriceTickModel>();
        using IDisposable subscription = PriceMonitor.SettledPrices(ticks, TimeSpan.FromMilliseconds(100), scheduler).Subscribe(settled.Add);

        for (int index = 0; index < 10; index++)
        {
            ticks.OnNext(Tick("X", index));
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(99).Ticks);
        }

        Assert.Empty(settled);
    }

    [Fact]
    public void Throttle_NegativeNegative_NullStreamOrScheduler_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => PriceMonitor.SettledPrices(null!, TimeSpan.FromSeconds(1), new TestScheduler()));
        Assert.Throws<ArgumentNullException>(() => PriceMonitor.SettledPrices(Observable.Empty<PriceTickModel>(), TimeSpan.FromSeconds(1), null!));
    }

    // ---------- CountPerWindow (Buffer by time) ----------

    [Fact]
    public void CountPerWindow_PositivePositive_CountsTicksInEachSecond()
    {
        var scheduler = new TestScheduler();
        ITestableObservable<PriceTickModel> ticks = scheduler.CreateHotObservable(
            ReactiveTest.OnNext(TimeSpan.FromMilliseconds(100).Ticks, Tick("X", 1m)),
            ReactiveTest.OnNext(TimeSpan.FromMilliseconds(900).Ticks, Tick("X", 2m)),
            ReactiveTest.OnNext(TimeSpan.FromMilliseconds(1500).Ticks, Tick("X", 3m)));
        var counts = new List<int>();
        using IDisposable subscription = PriceMonitor.CountPerWindow(ticks, TimeSpan.FromSeconds(1), scheduler).Subscribe(counts.Add);

        scheduler.AdvanceTo(TimeSpan.FromSeconds(3).Ticks);

        Assert.Equal([2, 1, 0], counts); // second 1: two ticks, second 2: one, second 3: none
    }

    [Fact]
    public void CountPerWindow_PositiveNegative_UnsubscribedEarly_StopsReceiving()
    {
        var scheduler = new TestScheduler();
        var counts = new List<int>();
        IDisposable subscription = PriceMonitor.CountPerWindow(Observable.Never<PriceTickModel>(), TimeSpan.FromSeconds(1), scheduler).Subscribe(counts.Add);

        scheduler.AdvanceTo(TimeSpan.FromSeconds(2).Ticks);
        subscription.Dispose();                               // unsubscribe = stop (and free the timer)
        scheduler.AdvanceTo(TimeSpan.FromSeconds(10).Ticks);

        Assert.Equal([0, 0], counts);
    }

    [Fact]
    public void CountPerWindow_NegativeNegative_NullStreamOrScheduler_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => PriceMonitor.CountPerWindow(null!, TimeSpan.FromSeconds(1), new TestScheduler()));
        Assert.Throws<ArgumentNullException>(() => PriceMonitor.CountPerWindow(Observable.Empty<PriceTickModel>(), TimeSpan.FromSeconds(1), null!));
    }
}
