using SidebarAndDateBinder.Data.Entities;

namespace SidebarAndDateBinder.Models;

// Everything the "Activity on <day>" page shows.
public class DailyActivityViewModel
{
    public DateOnly Day { get; set; }

    // Set when the ?day=... value was not a valid yyyy-MM-dd date. null = no error.
    public string? DateErrorMessage { get; set; }

    public List<ActivityEntry> Entries { get; set; } = new();
}
