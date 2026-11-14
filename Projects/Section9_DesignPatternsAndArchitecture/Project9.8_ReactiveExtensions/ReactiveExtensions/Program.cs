using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveExtensions.Models;
using ReactiveExtensions.Monitors;

// ---------- 1. IObservable without Rx operators ----------
// Observable.Create: write the "push" yourself. OnNext = an item, OnCompleted = the end, OnError = failure.
IObservable<int> countdown = Observable.Create<int>(observer =>
{
    for (int number = 3; number >= 1; number--)
    {
        observer.OnNext(number);
    }
    observer.OnCompleted();
    return Disposable.Empty; // what to do when the subscriber unsubscribes (nothing to clean up here)
});
Console.Write("1. Countdown:");
countdown.Subscribe(
    number => Console.Write($" {number}"),
    () => Console.WriteLine(" - done"));

// ---------- 2. A Subject: something you can push into by hand (like an event you raise) ----------
using var ticks = new Subject<PriceTickModel>();

using IDisposable jumps = PriceMonitor.WatchForJumps(ticks, percent: 5m)
    .Subscribe(jump => Console.WriteLine($"   JUMP {jump.Symbol}: {jump.From} -> {jump.To} ({jump.ChangePercent:+0.00;-0.00}%)"));
using IDisposable average = PriceMonitor.MovingAverage(ticks.Where(tick => tick.Symbol == "MSFT"), window: 3)
    .Subscribe(value => Console.WriteLine($"   MSFT average of last 3: {value}"));

Console.WriteLine("2. Live ticks:");
foreach ((string symbol, decimal price) in new[] { ("MSFT", 400m), ("AAPL", 200m), ("MSFT", 404m), ("AAPL", 189m), ("MSFT", 430m), ("MSFT", 431m) })
{
    Console.WriteLine($"   tick {symbol} {price}");
    ticks.OnNext(new PriceTickModel(symbol, price));
}

// ---------- 3. Time operators on a real clock ----------
// A burst of 5 ticks 20 ms apart, a pause, then 2 more. Throttle(100 ms) keeps only the last tick of each burst.
Console.WriteLine("3. Throttle - only settled prices (quiet for 100 ms):");
IObservable<PriceTickModel> bursts = Observable.Interval(TimeSpan.FromMilliseconds(20))
    .Take(5).Select(index => new PriceTickModel("MSFT", 400m + index))
    .Concat(Observable.Timer(TimeSpan.FromMilliseconds(300)).Select(_ => new PriceTickModel("MSFT", 410m)))
    .Concat(Observable.Interval(TimeSpan.FromMilliseconds(20)).Take(2).Select(index => new PriceTickModel("MSFT", 420m + index)));

IList<PriceTickModel> settled = await PriceMonitor.SettledPrices(bursts, TimeSpan.FromMilliseconds(100), Scheduler.Default).ToList();
Console.WriteLine($"   7 ticks in, {settled.Count} out: {string.Join(", ", settled.Select(tick => tick.Price))}");
