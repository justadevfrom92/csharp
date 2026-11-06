using SessionShoppingCart.Data.Entities;
using SessionShoppingCart.SessionState;

namespace SessionShoppingCart.Models;

// Everything the cart page shows: what can be bought, and what's in the cart.
public class CartPageViewModel
{
    public IReadOnlyList<ShopItem> ItemsForSale { get; set; } = [];
    public ShoppingCart Cart { get; set; } = new();
}
