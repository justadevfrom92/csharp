using JsonYamlConfiguration.Loaders;
using JsonYamlConfiguration.Settings;

string folder = AppContext.BaseDirectory; // shop.json and shop.Production.yaml are copied next to the program

foreach (string environment in new[] { "Development", "Production" })
{
    ShopSettings settings = ShopSettingsLoader.Load(folder, environment);
    Console.WriteLine($"--- {environment} ---");
    Console.WriteLine($"  Name:               {settings.Name}");
    Console.WriteLine($"  Max items/order:    {settings.MaximumItemsPerOrder}");
    Console.WriteLine($"  Free shipping from: {settings.Shipping.FreeShippingFrom:0.00}");
    Console.WriteLine($"  Countries:          {string.Join(", ", settings.Shipping.Countries)}");
    Console.WriteLine($"  Discount codes:     {string.Join(", ", settings.DiscountCodes.Select(code => $"{code.Key}={code.Value}%"))}");
}

Console.WriteLine();
Console.WriteLine($"One value by path, Shop:Shipping:Countries:0 = {ShopSettingsLoader.ReadValue(folder, "Production", "Shop:Shipping:Countries:0")}");

ShopSettings overridden = ShopSettingsLoader.Load(folder, "Production", [new("Shop:Name", "Set from code")]);
Console.WriteLine($"Override wins over both files: {overridden.Name}");
