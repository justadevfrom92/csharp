using System.ComponentModel.DataAnnotations;

namespace SharedCacheAndSettings.Settings;

// Matches the "Shop" section in appsettings.json. Loaded once at startup.
public class ShopSettings
{
    [Required]
    public string ShopName { get; set; } = "";

    [EmailAddress]
    public string SupportEmail { get; set; } = "";

    [Range(1, 60)]
    public int CategoryCacheMinutes { get; set; }

    [Range(1, 10_000)]
    public int MaximumCachedItems { get; set; }
}
