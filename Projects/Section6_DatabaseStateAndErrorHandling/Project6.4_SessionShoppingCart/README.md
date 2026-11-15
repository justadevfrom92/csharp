# Project 6.4 — Session Shopping Cart

> New to `.cshtml` files (`@model`, `@foreach`, `ViewBag`, layouts)? Read [RAZOR_BASICS.md](../../RAZOR_BASICS.md) first.

```bash
dotnet dev-certs https --trust
```

```bash
dotnet run
```
Open `https://localhost:7064`, add items, refresh, open a second browser — each has its own cart.

## What it covers

- `AddSession` / `UseSession`, `HttpContext.Session`
- Storing a whole object (`ShoppingCartModel`) as JSON with `SetObjectAsJson` / `GetObjectFromJson`
- Cookie flags: `HttpOnly`, `Secure`, `SameSite`, plus HTTPS redirect and HSTS

## Where the data is, and why it's safe

- The **cart lives on the server** (in memory here; Redis or SQL in a multi-server setup).
- The browser only gets a cookie with a **session id**, and that cookie is **encrypted and signed** by
  ASP.NET Core Data Protection. A user can't read it or change it to someone else's id.
- Prices are always taken from the server's item list, never from the form, so a user can't
  edit the HTML and buy a monitor for 0.01.

## Security review of the settings

| Setting | Value | Why |
|---|---|---|
| `IdleTimeout` | 20 min | Short enough that a cart left on a shared/library computer doesn't stay forever; long enough that people don't lose their cart while browsing. Each request resets the timer. |
| `Cookie.HttpOnly` | `true` | JavaScript can't read the cookie. Even if an attacker injects a script (XSS), they can't steal the session id. |
| `Cookie.SecurePolicy` | `Always` | Cookie is only sent over HTTPS, so nobody on the same Wi-Fi can read it from the network. |
| `Cookie.SameSite` | `Lax` | The browser doesn't send the cookie with POSTs started from other websites. That blocks most CSRF attacks. (`Strict` would also drop it when following a link from another site, so users would arrive with an "empty" cart.) |
| `Cookie.IsEssential` | `true` | The cart can't work without it, so it's allowed even before a cookie-consent banner is accepted. |
| `UseHttpsRedirection` + `UseHsts` | on | Forces HTTPS so `Secure` cookies actually get sent. HSTS tells browsers to never even try HTTP. |
| `[ValidateAntiForgeryToken]` | on every POST | Second layer of CSRF protection on top of `SameSite`. |

**What should NOT go in session:** passwords, card numbers, or anything that must survive a server
restart. In-memory session is lost when the app restarts and isn't shared between servers.
**Session vs login:** session is for temporary state like a cart. Who the user *is* belongs in an
authentication cookie (Project 7.1).

## Code snippets

**Turn on session, with a safe cookie**, from `Program.cs`

```csharp
builder.Services.AddDistributedMemoryCache(); // where session data is kept (server memory)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.Name = ".Roadmap.Cart";
    options.Cookie.HttpOnly = true;                           // JavaScript can't read it
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // only sent over HTTPS
    options.Cookie.SameSite = SameSiteMode.Lax;               // not sent on cross-site POSTs
    options.Cookie.IsEssential = true;
});

app.UseSession(); // before the controllers
```

**Extension methods: store any object in session as JSON**, from `Extensions/SessionJsonExtensions.cs`

```csharp
public static class SessionJsonExtensions
{
    public static void SetObjectAsJson<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }

    public static T? GetObjectFromJson<T>(this ISession session, string key)
    {
        string? json = session.GetString(key);
        if (json == null)
        {
            return default;
        }
        return JsonSerializer.Deserialize<T>(json);
    }
}
```

**Load, change, save the cart**, from `Controllers/CartController.cs`

```csharp
private ShoppingCartModel LoadCart()
{
    return HttpContext.Session.GetObjectFromJson<ShoppingCartModel>(CartSessionKey) ?? new ShoppingCartModel();
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
        HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
    }
    return RedirectToAction(nameof(Index));
}

HttpContext.Session.Clear(); // empty the cart
```

**A calculated property (not stored, worked out each time)**, from `Models/ShoppingCartModel.cs`

```csharp
public decimal Total
{
    get
    {
        return Lines.Sum(line => line.Price * line.Quantity);
    }
}
```

## Variations

Where the cart is kept, three ways. Cookie versions are in `Variations/CartStorageVariation.cs` (`/variations/cart/...`);
results below are from `curl`, including a user **editing** their cookie.

| Storage | What the browser holds | User edits the Monitor's price 180 → 0.01 | The difference |
|---|---|---|---|
| **Session** (original) | a random session id | nothing to edit: prices live on the server | server memory/Redis; lost when the session expires |
| **plain cookie** (`/plain`) | the cart JSON, prices included | **accepted: total 0.01** | readable and editable by the user; never trust it |
| **protected cookie** (`/protected`, `IDataProtector.Protect`) | `CfDJ8HXOPZInQBlM...` (encrypted + signed) | rejected: `Unprotect` threw, cart ignored | can't be read or changed; but max ~4 KB, sent with every request, and an old copy can be replayed |

Rule from the original that matters for all of them: the **price comes from the server's list** when an item is added,
never from the form or the cookie.

Other storage choices (not coded here): a **database table** keyed by user id (survives logins on other devices) and a
**distributed cache** like Redis (7.9) behind session when there are several servers.

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| `record ShopItemModel` | Catalog items are fixed data. | — |
| `class CartLineModel` with setters | `Quantity` changes, and `System.Text.Json` must set properties when reading the cart back from session. | A record's constructor-only properties work with JSON too, but `Quantity++` wouldn't. |
| `class ShoppingCartModel`, `List<CartLineModel> Lines { get; set; }` | Lines are added and removed; the JSON reader needs a settable list. | `IReadOnlyList` can't be filled by the deserializer or changed by `AddItem`. |
| `decimal Total { get { ... } }` (computed) | Always worked out from the lines, never stored. | A stored total can go out of sync with the lines. |
| `static class SessionJsonExtensions` with generic `<T>` | Extension methods must be in a static class; `<T>` makes them work for any type, not just carts. | A cart-only helper would be copied for the next object. |
| `T?` return / `default` | No value in session → `null`, and the caller uses `?? new ShoppingCartModel()`. | — |
| `static readonly List<ShopItemModel> itemsForSale` | Fixed demo catalog. | Could be an `IReadOnlyList` or array to make "never changes" explicit — fair improvement. |
| `const string CartSessionKey` | One key name used everywhere. | — |
