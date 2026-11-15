using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using SessionShoppingCart.Models;

namespace SessionShoppingCart.Variations;

// Where to keep the cart. ORIGINAL: session - the cart stays on the SERVER, the browser only holds a random id.
// Here the cart lives in a COOKIE instead (on the user's computer), two ways. Try with curl (see README).
// GET is used only so it's easy to try; real "add" actions are POST + anti-forgery token (see CartController).
[Route("variations/cart")]
public class CartStorageVariation : Controller
{
    private const string PlainCookie = "cart-plain";
    private const string ProtectedCookie = "cart-protected";
    private readonly IDataProtector protector;

    public CartStorageVariation(IDataProtectionProvider protection)
    {
        // Data Protection: ASP.NET Core's built-in encryption (the same thing that protects auth and session cookies).
        // The "purpose" string separates keys, so a value protected for one purpose can't be unprotected for another.
        protector = protection.CreateProtector("CartStorageVariation.v1");
    }

    // PLAIN cookie: the cart's JSON, prices included, readable AND EDITABLE by the user.
    [HttpGet("plain/add")]
    public IActionResult AddPlain(int itemId, string name, decimal price)
    {
        List<CartLineModel> cart = ReadPlain();
        cart.Add(new CartLineModel { ItemId = itemId, Name = name, Price = price, Quantity = 1 });
        Response.Cookies.Append(PlainCookie, JsonSerializer.Serialize(cart), new CookieOptions { HttpOnly = true });
        return Ok(cart);
    }

    [HttpGet("plain")]
    public IActionResult ViewPlain()
    {
        List<CartLineModel> cart = ReadPlain();
        return Ok(new { lines = cart, total = cart.Sum(line => line.Price * line.Quantity) });
    }

    // PROTECTED cookie: encrypted + signed. The user can't read it, and any change makes Unprotect THROW.
    // Still on the user's computer: limited to ~4 KB, sent with every request, and an OLD copy can be replayed.
    [HttpGet("protected/add")]
    public IActionResult AddProtected(int itemId, string name, decimal price)
    {
        List<CartLineModel> cart = ReadProtected(out _);
        cart.Add(new CartLineModel { ItemId = itemId, Name = name, Price = price, Quantity = 1 });
        Response.Cookies.Append(ProtectedCookie, protector.Protect(JsonSerializer.Serialize(cart)), new CookieOptions { HttpOnly = true });
        return Ok(cart);
    }

    [HttpGet("protected")]
    public IActionResult ViewProtected()
    {
        List<CartLineModel> cart = ReadProtected(out string? problem);
        return Ok(new { lines = cart, total = cart.Sum(line => line.Price * line.Quantity), problem });
    }

    private List<CartLineModel> ReadPlain()
    {
        string? json = Request.Cookies[PlainCookie];
        return json == null ? new List<CartLineModel>() : JsonSerializer.Deserialize<List<CartLineModel>>(json) ?? new List<CartLineModel>();
    }

    private List<CartLineModel> ReadProtected(out string? problem)
    {
        problem = null;
        string? value = Request.Cookies[ProtectedCookie];
        if (value == null)
        {
            return new List<CartLineModel>();
        }
        try
        {
            return JsonSerializer.Deserialize<List<CartLineModel>>(protector.Unprotect(value)) ?? new List<CartLineModel>();
        }
        catch (CryptographicException)
        {
            problem = "cookie was changed - ignored";
            return new List<CartLineModel>();
        }
    }
}
