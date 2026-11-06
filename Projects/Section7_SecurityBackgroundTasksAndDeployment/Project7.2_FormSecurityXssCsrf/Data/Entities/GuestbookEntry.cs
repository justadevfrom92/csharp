namespace FormSecurityXssCsrf.Data.Entities;

// One guestbook message as it is STORED. Stored exactly as typed; encoding happens when it's shown.
public class GuestbookEntry
{
    public string Name { get; set; } = "";
    public string Message { get; set; } = "";
    public string? Website { get; set; }
    public DateTime PostedAtUtc { get; set; }
}
