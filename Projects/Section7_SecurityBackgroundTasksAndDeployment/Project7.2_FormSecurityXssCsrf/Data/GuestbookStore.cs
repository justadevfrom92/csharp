using FormSecurityXssCsrf.Data.Entities;

namespace FormSecurityXssCsrf.Data;

// In-memory list of entries shared by all requests.
public static class GuestbookStore
{
    private static readonly List<GuestbookEntry> entries = new();
    private static readonly object listLock = new();

    public static void Add(GuestbookEntry entry)
    {
        lock (listLock)
        {
            entries.Add(entry);
        }
    }

    public static List<GuestbookEntry> GetNewestFirst()
    {
        lock (listLock)
        {
            return entries.OrderByDescending(entry => entry.PostedAtUtc).ToList();
        }
    }
}
