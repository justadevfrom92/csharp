using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveExtensions.Models;

namespace ReactiveExtensions.Variations;

// Other Rx operators and subjects next to the ones PriceMonitor (original) uses.
public static class PriceStreamVariation
{
    // SAMPLE: every "period", pass on the LATEST tick (if a new one arrived). A constant stream gives a steady
    // trickle. THROTTLE (original SettledPrices) waits for QUIET - a constant stream gives NOTHING.
    public static IObservable<PriceTickModel> SampledPrices(IObservable<PriceTickModel> ticks, TimeSpan period, IScheduler scheduler)
    {
        ArgumentNullException.ThrowIfNull(ticks);
        return ticks.Sample(period, scheduler);
    }

    // DISTINCT UNTIL CHANGED: drop a tick when the price is the same as the previous one.
    public static IObservable<PriceTickModel> OnlyChanges(IObservable<PriceTickModel> ticks)
    {
        ArgumentNullException.ThrowIfNull(ticks);
        return ticks.DistinctUntilChanged(tick => tick.Price);
    }

    // What a LATE subscriber receives, for three kinds of subject. 3 values are pushed, THEN it subscribes,
    // then 1 more value is pushed.
    //   Subject<T>          - only values AFTER subscribing (like a live radio)
    //   BehaviorSubject<T>  - the LATEST value straight away, then new ones ("current price")
    //   ReplaySubject<T>    - ALL earlier values, then new ones (or the last N with new ReplaySubject<T>(N))
    public static List<decimal> LateSubscriberSees(ISubject<decimal> subject)
    {
        ArgumentNullException.ThrowIfNull(subject);
        subject.OnNext(1m);
        subject.OnNext(2m);
        subject.OnNext(3m);
        var seen = new List<decimal>();
        using IDisposable subscription = subject.Subscribe(seen.Add);
        subject.OnNext(4m);
        return seen;
    }

    // COLD vs HOT: Observable.Interval is COLD - each subscriber gets its OWN timer starting at 0.
    // Publish().RefCount() makes it HOT - subscribers SHARE one timer, and a late one joins mid-way.
    // advanceTime moves the clock forward (a TestScheduler in the tests; the app would just wait).
    public static (List<long> FirstCold, List<long> LateCold, List<long> LateHot) ColdVersusHot(IScheduler scheduler, Action<TimeSpan> advanceTime)
    {
        ArgumentNullException.ThrowIfNull(advanceTime);
        IObservable<long> cold = Observable.Interval(TimeSpan.FromSeconds(1), scheduler).Take(4);
        IObservable<long> hot = cold.Publish().RefCount();
        var firstCold = new List<long>();
        var lateCold = new List<long>();
        var lateHot = new List<long>();
        using IDisposable first = cold.Subscribe(firstCold.Add);
        using IDisposable firstShared = hot.Subscribe(_ => { });
        advanceTime(TimeSpan.FromSeconds(2.5));   // 2 values have gone by
        using IDisposable late = cold.Subscribe(lateCold.Add);
        using IDisposable lateShared = hot.Subscribe(lateHot.Add);
        advanceTime(TimeSpan.FromSeconds(10));
        return (firstCold, lateCold, lateHot);
    }
}
