using MvcSidebarViewComponentAndDateBinder.Models;

namespace MvcSidebarViewComponentAndDateBinder.Services;

public interface IRecentActivityFeedService
{
    Task<IReadOnlyList<RecentActivityEntry>> GetMostRecentEntriesAsync(int maximumEntryCount, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecentActivityEntry>> GetEntriesWithinDateRangeAsync(DateOnly? inclusiveStartDate, DateOnly? inclusiveEndDate, ActivityKind? kindFilter, CancellationToken cancellationToken = default);
    Task RecordActivityAsync(ActivityKind kind, string actorDisplayName, string activityDescription, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory feed (Singleton). Methods are async-shaped on purpose: the view component and
/// controllers are written against an async contract so swapping in a database or HTTP-backed
/// feed later needs no caller changes.
/// </summary>
public sealed class InMemoryRecentActivityFeedService : IRecentActivityFeedService
{
    private const int MaximumRetainedEntries = 500;
    private readonly Lock _feedLock = new();
    private readonly LinkedList<RecentActivityEntry> _entriesNewestFirst = new();
    private readonly TimeProvider _timeProvider;

    public InMemoryRecentActivityFeedService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        SeedHistoricalActivity();
    }

    public Task<IReadOnlyList<RecentActivityEntry>> GetMostRecentEntriesAsync(int maximumEntryCount, CancellationToken cancellationToken = default)
    {
        lock (_feedLock)
        {
            IReadOnlyList<RecentActivityEntry> snapshot = _entriesNewestFirst.Take(Math.Clamp(maximumEntryCount, 1, 50)).ToList();
            return Task.FromResult(snapshot);
        }
    }

    public Task<IReadOnlyList<RecentActivityEntry>> GetEntriesWithinDateRangeAsync(DateOnly? inclusiveStartDate, DateOnly? inclusiveEndDate, ActivityKind? kindFilter, CancellationToken cancellationToken = default)
    {
        lock (_feedLock)
        {
            IReadOnlyList<RecentActivityEntry> matchingEntries = _entriesNewestFirst
                .Where(entry => inclusiveStartDate is null || DateOnly.FromDateTime(entry.OccurredAt.UtcDateTime) >= inclusiveStartDate)
                .Where(entry => inclusiveEndDate is null || DateOnly.FromDateTime(entry.OccurredAt.UtcDateTime) <= inclusiveEndDate)
                .Where(entry => kindFilter is null || entry.Kind == kindFilter)
                .ToList();
            return Task.FromResult(matchingEntries);
        }
    }

    public Task RecordActivityAsync(ActivityKind kind, string actorDisplayName, string activityDescription, CancellationToken cancellationToken = default)
    {
        AddEntry(new RecentActivityEntry(Guid.NewGuid(), kind, actorDisplayName, activityDescription, _timeProvider.GetUtcNow()));
        return Task.CompletedTask;
    }

    private void AddEntry(RecentActivityEntry entry)
    {
        lock (_feedLock)
        {
            _entriesNewestFirst.AddFirst(entry);
            // Bounded history: a Singleton that grows forever is a memory leak.
            while (_entriesNewestFirst.Count > MaximumRetainedEntries)
            {
                _entriesNewestFirst.RemoveLast();
            }
        }
    }

    private void SeedHistoricalActivity()
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        (ActivityKind Kind, string Actor, string Description, TimeSpan Age)[] seedEntries =
        [
            (ActivityKind.UserRegistered, "Riley", "Created an account", TimeSpan.FromDays(6)),
            (ActivityKind.OrderPlaced, "Riley", "Placed order #1042 ($84.10)", TimeSpan.FromDays(5)),
            (ActivityKind.InvoicePaid, "Morgan", "Paid invoice INV-2207", TimeSpan.FromDays(3)),
            (ActivityKind.ProductUpdated, "Sam (admin)", "Changed price of Wireless Mouse", TimeSpan.FromDays(2)),
            (ActivityKind.CommentPosted, "Jordan", "Reviewed Cast Iron Skillet ★★★★★", TimeSpan.FromDays(1)),
            (ActivityKind.OrderPlaced, "Jordan", "Placed order #1043 ($39.50)", TimeSpan.FromHours(20)),
            (ActivityKind.OrderPlaced, "Quinn", "Placed order #1044 ($212.00)", TimeSpan.FromHours(3)),
        ];
        foreach (var seedEntry in seedEntries.OrderByDescending(entry => entry.Age))
        {
            AddEntry(new RecentActivityEntry(Guid.NewGuid(), seedEntry.Kind, seedEntry.Actor, seedEntry.Description, now - seedEntry.Age));
        }
    }
}
