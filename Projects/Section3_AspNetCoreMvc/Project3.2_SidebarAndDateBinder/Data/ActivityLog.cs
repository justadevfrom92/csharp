using SidebarAndDateBinder.Data.Entities;

namespace SidebarAndDateBinder.Data;

// In-memory activity list shared by the whole app.
public static class ActivityLog
{
    private static readonly List<ActivityEntry> entries = new();
    private static readonly object listLock = new();

    static ActivityLog()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        entries.Add(new ActivityEntry(today.AddDays(-1), new TimeOnly(9, 15), "Logged in"));
        entries.Add(new ActivityEntry(today, new TimeOnly(10, 30), "Added product 'Keyboard'"));
        entries.Add(new ActivityEntry(today, new TimeOnly(11, 0), "Changed price of 'Desk'"));
    }

    public static void Add(string text)
    {
        lock (listLock)
        {
            entries.Add(new ActivityEntry(DateOnly.FromDateTime(DateTime.Now), TimeOnly.FromDateTime(DateTime.Now), text));
        }
    }

    public static List<ActivityEntry> GetNewest(int count)
    {
        lock (listLock)
        {
            return entries.OrderByDescending(entry => entry.Date).ThenByDescending(entry => entry.Time).Take(count).ToList();
        }
    }

    public static List<ActivityEntry> GetForDay(DateOnly day)
    {
        lock (listLock)
        {
            return entries.Where(entry => entry.Date == day).OrderBy(entry => entry.Time).ToList();
        }
    }
}
