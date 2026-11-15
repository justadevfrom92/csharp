using System.ComponentModel.DataAnnotations;

namespace ServiceRegistration.Settings;

public class ShopSettings
{
    [Required]
    public string ShopName { get; set; } = "";

    [EmailAddress]
    public string SupportEmail { get; set; } = "";
}
