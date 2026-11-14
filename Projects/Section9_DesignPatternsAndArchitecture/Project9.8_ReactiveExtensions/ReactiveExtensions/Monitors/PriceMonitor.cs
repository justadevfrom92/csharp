using System.Reactive.Concurrency;
using System.Reactive.Linq;
using ReactiveExtensions.Models;

namespace ReactiveExtensions.Monitors;

// REACTIVE EXTENSIONS (Rx): IEnumerable<T> is data you PULL ("give me the next item").
// IObservable<T> is data that is PUSHED to you over TIME ("here's the next item, whenever it happens"):
// price ticks, clicks, sensor readings, messages. Rx gives IObservable the same LINQ operators
// (Where, Select, GroupBy) PLUS operators about time (Throttle, Buffer by time, Sample...).
//
// Every method here takes a stream and returns a NEW stream. Nothing runs until someone Subscribes.
// Time-based methods take an IScheduler, so tests can use a fake clock (TestScheduler) and run instantly.
public static class PriceMonitor
{
    // Alerts when a stock moves by at least "percent" between two ticks IN A ROW (per symbol).
    public static IObservable<PriceJumpModel> WatchForJumps(IObservable<PriceTickModel> ticks, decimal percent)
    {
        ArgumentNullException.ThrowIfNull(ticks);
        if (percent <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), "Percent must be more than 0.");
        }
        return ticks
            .GroupBy(tick => tick.Symbol)                         // one sub-stream per symbol
            .SelectMany(symbolTicks => symbolTicks
                .Buffer(2, 1)                                     // sliding pairs: [t1,t2], [t2,t3], ...
                .Where(pair => pair.Count == 2)                   // the last "pair" at the end has only 1
                .Select(pair => new PriceJumpModel(pair[0].Symbol, pair[0].Price, pair[1].Price,
                    Math.Round((pair[1].Price - pair[0].Price) / pair[0].Price * 100m, 2))))
            .Where(jump => Math.Abs(jump.ChangePercent) >= percent);
    }

    // Average of the last "window" prices, updated on every new tick (all symbols together).
    public static IObservable<decimal> MovingAverage(IObservable<PriceTickModel> ticks, int window)
    {
        ArgumentNullException.ThrowIfNull(ticks);
        if (window < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(window), "Window must be at least 1.");
        }
        return ticks
            .Buffer(window, 1)
            .Where(prices => prices.Count == window)
            .Select(prices => Math.Round(prices.Average(tick => tick.Price), 2));
    }

    // THROTTLE (also called "debounce"): only pass a tick on once the stream has been QUIET for "quiet".
    // A burst of 50 ticks in one second becomes ONE update - e.g. to avoid redrawing a screen 50 times.
    public static IObservable<PriceTickModel> SettledPrices(IObservable<PriceTickModel> ticks, TimeSpan quiet, IScheduler scheduler)
    {
        ArgumentNullException.ThrowIfNull(ticks);
        ArgumentNullException.ThrowIfNull(scheduler);
        return ticks.Throttle(quiet, scheduler);
    }

    // How many ticks arrived in each time window (e.g. "ticks per second").
    public static IObservable<int> CountPerWindow(IObservable<PriceTickModel> ticks, TimeSpan window, IScheduler scheduler)
    {
        ArgumentNullException.ThrowIfNull(ticks);
        ArgumentNullException.ThrowIfNull(scheduler);
        return ticks.Buffer(window, scheduler).Select(batch => batch.Count);
    }
}
