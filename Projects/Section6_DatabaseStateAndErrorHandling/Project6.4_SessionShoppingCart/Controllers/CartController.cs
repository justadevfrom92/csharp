using Microsoft.AspNetCore.Mvc;
using SessionShoppingCart.Data;
using SessionShoppingCart.Data.Entities;
using SessionShoppingCart.Extensions;
using SessionShoppingCart.Models;
using SessionShoppingCart.SessionState;

namespace SessionShoppingCart.Controllers;

public class CartController : Controller
{
    private const string CartSessionKey = "ShoppingCart";

    public IActionResult Index()
    {
        var page = new CartPageViewModel
        {
            ItemsForSale = ShopCatalog.ItemsForSale,
            Cart = LoadCart(),
        };
        return View(page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Add(int itemId)
    {
        ShopItem? item = ShopCatalog.FindById(itemId);
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
