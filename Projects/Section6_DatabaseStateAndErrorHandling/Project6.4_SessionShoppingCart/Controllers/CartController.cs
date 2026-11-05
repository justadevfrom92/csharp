using Microsoft.AspNetCore.Mvc;
using SessionShoppingCart.Models;

namespace SessionShoppingCart.Controllers;

public class CartController : Controller
{
    private const string CartSessionKey = "ShoppingCart";

    private static readonly List<ShopItem> itemsForSale =
    [
        new ShopItem(1, "Keyboard", 25.00m),
        new ShopItem(2, "Mouse", 12.50m),
        new ShopItem(3, "Monitor", 180.00m),
    ];

    public IActionResult Index()
    {
        ViewBag.ItemsForSale = itemsForSale;
        return View(LoadCart());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Add(int itemId)
    {
        ShopItem? item = itemsForSale.FirstOrDefault(existing => existing.Id == itemId);
        if (item != null)
        {
            ShoppingCart cart = LoadCart();
            cart.AddItem(item); // price comes from the SERVER list, never from the form
            SaveCart(cart);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int itemId)
    {
        ShoppingCart cart = LoadCart();
        cart.RemoveItem(itemId);
        SaveCart(cart);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Index));
    }

    private ShoppingCart LoadCart()
    {
        return HttpContext.Session.GetObjectFromJson<ShoppingCart>(CartSessionKey) ?? new ShoppingCart();
    }

    private void SaveCart(ShoppingCart cart)
    {
        HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
    }
}
