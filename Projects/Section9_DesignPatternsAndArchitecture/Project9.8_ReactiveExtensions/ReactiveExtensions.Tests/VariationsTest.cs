using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Reactive.Testing;
using ReactiveExtensions.Models;
using ReactiveExtensions.Monitors;
using ReactiveExtensions.Variations;

namespace ReactiveExtensions.Tests;

// Throttle vs Sample, DistinctUntilChanged, three kinds of subject, cold vs hot
// (ReactiveExtensions/Variations/PriceStreamVariation.cs). Time-based ones use a fake clock.
public class VariationsTest
{
    [Fact]
    public void Timing_PositiveNegative_ConstantStream_ThrottleGivesNothing_SampleGivesASteadyTrickle()
    {
        var scheduler = new TestScheduler();
        var ticks = new Subject<PriceTickModel>();
        var throttled = new List<PriceTickModel>();
        var sampled = new List<PriceTickModel>();
        using IDisposable a = PriceMonitor.SettledPrices(ticks, TimeSpan.FromMilliseconds(100), scheduler).Subscribe(throttled.Add);
        using IDisposable b = PriceStreamVariation.SampledPrices(ticks, TimeSpan.FromMilliseconds(100), scheduler).Subscribe(sampled.Add);

        for (int index = 0; index < 20; index++) // a tick every 50 ms for 1 second: never quiet for 100 ms
        {
            ticks.OnNext(new PriceTickModel("X", index));
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(50).Ticks);
        }

        Assert.Empty(throttled);             // Throttle waits for quiet that never comes
        Assert.Equal(10, sampled.Count);     // Sample: the latest value every 100 ms
    }

    [Fact]
    public void Filtering_PositivePositive_DistinctUntilChanged_DropsRepeatsOnly()
    {
        var seen = new List<decimal>();
        using IDisposable subscription = PriceStreamVariation.OnlyChanges(new[] { 1m, 1m, 2m, 2m, 1m }.Select(price => new PriceTickModel("X", price)).ToObservable())
            .Subscribe(tick => seen.Add(tick.Price));

        Assert.Equal([1m, 2m, 1m], seen); // the second 1 is kept: it's a change from 2
    }

    [Fact]
    public void Subjects_PositiveNegative_LateSubscriber_SeesDifferentAmountsOfThePast()
    {
        Assert.Equal([4m], PriceStreamVariation.LateSubscriberSees(new Subject<decimal>()));
        Assert.Equal([3m, 4m], PriceStreamVariation.LateSubscriberSees(new BehaviorSubject<decimal>(0m)));
        Assert.Equal([1m, 2m, 3m, 4m], PriceStreamVariation.LateSubscriberSees(new ReplaySubject<decimal>()));
        Assert.Equal([2m, 3m, 4m], PriceStreamVariation.LateSubscriberSees(new ReplaySubject<decimal>(2)));
    }

    [Fact]
    public void ColdHot_PositiveNegative_LateSubscriber_ColdStartsAgain_HotJoinsMidWay()
    {
        var scheduler = new TestScheduler();

        (List<long> firstCold, List<long> lateCold, List<long> lateHot) = PriceStreamVariation.ColdVersusHot(scheduler, time => scheduler.AdvanceBy(time.Ticks));

        Assert.Equal([0L, 1L, 2L, 3L], firstCold);
        Assert.Equal([0L, 1L, 2L, 3L], lateCold); // its OWN timer, from 0
        Assert.Equal([2L, 3L], lateHot);          // the SHARED timer: 0 and 1 already went by
    }

    [Fact]
    public void Variations_NegativeNegative_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => PriceStreamVariation.SampledPrices(null!, TimeSpan.FromSeconds(1), new TestScheduler()));
        Assert.Throws<ArgumentNullException>(() => PriceStreamVariation.OnlyChanges(null!));
        Assert.Throws<ArgumentNullException>(() => PriceStreamVariation.LateSubscriberSees(null!));
    }
}
