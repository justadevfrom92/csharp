using SessionShoppingCart.Data.Entities;

namespace SessionShoppingCart.Data;

// The items for sale. Fixed demo data; a real shop would load these from a database.
public static class ShopCatalog
{
    public static readonly IReadOnlyList<ShopItem> ItemsForSale =
    [
        new ShopItem(1, "Keyboard", 25.00m),
        new ShopItem(2, "Mouse", 12.50m),
        new ShopItem(3, "Monitor", 180.00m),
    ];

    public static ShopItem? FindById(int itemId)
    {
        return ItemsForSale.FirstOrDefault(item => item.Id == itemId);
    }
}
