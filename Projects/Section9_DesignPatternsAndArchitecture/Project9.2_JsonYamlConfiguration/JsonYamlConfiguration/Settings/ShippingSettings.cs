using System.ComponentModel.DataAnnotations;

namespace JsonYamlConfiguration.Settings;

// Matches the "Shop:Shipping" section.
public class ShippingSettings
{
    [Range(0, 10_000)]
    public decimal FreeShippingFrom { get; set; }

    [MinLength(1, ErrorMessage = "Ship to at least one country.")]
    public List<string> Countries { get; set; } = new();
}
