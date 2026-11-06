using Microsoft.AspNetCore.Mvc;
using SessionShoppingCart.Extensions;
using SessionShoppingCart.Models;

namespace SessionShoppingCart.Controllers;

public class CartController : Controller
{
    private const string CartSessionKey = "ShoppingCart";

    private static readonly List<ShopItemModel> itemsForSale =
    [
        new ShopItemModel(1, "Keyboard", 25.00m),
        new ShopItemModel(2, "Mouse", 12.50m),
        new ShopItemModel(3, "Monitor", 180.00m),
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
        ShopItemModel? item = itemsForSale.FirstOrDefault(existing => existing.Id == itemId);
        if (item != null)
        {
            ShoppingCartModel cart = LoadCart();
            cart.AddItem(item); // price comes from the SERVER list, never from the form
            SaveCart(cart);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int itemId)
    {
        ShoppingCartModel cart = LoadCart();
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

    private ShoppingCartModel LoadCart()
    {
        return HttpContext.Session.GetObjectFromJson<ShoppingCartModel>(CartSessionKey) ?? new ShoppingCartModel();
    }

    private void SaveCart(ShoppingCartModel cart)
    {
        HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
    }
}
