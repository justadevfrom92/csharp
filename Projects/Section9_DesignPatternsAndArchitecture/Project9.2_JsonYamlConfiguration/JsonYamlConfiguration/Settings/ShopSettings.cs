using System.ComponentModel.DataAnnotations;

namespace JsonYamlConfiguration.Settings;

// Matches the "Shop" section. The binder fills properties by NAME (not case-sensitive):
//   Shop:Name -> Name,  Shop:Shipping:Countries:0 -> Shipping.Countries[0],  Shop:DiscountCodes:VIP25 -> DiscountCodes["VIP25"]
public class ShopSettings
{
    [Required]
    [StringLength(60)]
    public string Name { get; set; } = "";

    [Range(1, 100)]
    public int MaximumItemsPerOrder { get; set; }

    public ShippingSettings Shipping { get; set; } = new();

    // A JSON/YAML object with free key names becomes a Dictionary.
    public Dictionary<string, int> DiscountCodes { get; set; } = new();
}
