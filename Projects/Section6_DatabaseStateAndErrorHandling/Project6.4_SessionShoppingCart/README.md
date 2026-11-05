# Project 6.4 — Session Shopping Cart

```bash
dotnet dev-certs https --trust
```

```bash
dotnet run
```
Open `https://localhost:7064`, add items, refresh, open a second browser — each has its own cart.

## What it covers

- `AddSession` / `UseSession`, `HttpContext.Session`
- Storing a whole object (`ShoppingCart`) as JSON with `SetObjectAsJson` / `GetObjectFromJson`
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
