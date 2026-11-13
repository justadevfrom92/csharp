# Project 7.1 — Cookie Login & Role-Based Access

> New to `.cshtml` files (`@model`, `@foreach`, `ViewBag`, layouts)? Read [RAZOR_BASICS.md](../../RAZOR_BASICS.md) first.

```bash
dotnet dev-certs https --trust
```

| Folder | What |
|---|---|
| `CookieAuthAndRoles/` | The web app |
| `CookieAuthAndRoles.Tests/` | xUnit tests: login, return URLs, admin access, login-required, register, logout (app runs in memory) |

```bash
dotnet run --project CookieAuthAndRoles
```

```bash
dotnet test CookieAuthAndRoles.Tests
```
Open `https://localhost:7071` (HTTPS is required: the cookies are marked `Secure`).
Demo users: `admin / Admin123!` (Admin role), `alice / Alice123!` (User role). You can also register.

| Page | Who can see it |
|---|---|
| `/` | Everyone (`[AllowAnonymous]`) |
| `/Account/Login`, `/Account/Register` | Everyone |
| `/Account/Profile` | Any logged-in user (global filter) |
| `/Admin` | Only the Admin role (`[Authorize(Roles = "Admin")]`) |

Not logged in → sent to the login page. Logged in but wrong role → "Access denied" page.

## What it covers

- `AddAuthentication().AddCookie(...)`, `SignInAsync` with claims (`Name`, `Role`), `SignOutAsync`.
- `UseAuthentication()` then `UseAuthorization()`.
- `[Authorize]`, `[Authorize(Roles = "...")]`, `[AllowAnonymous]`, plus a global `AuthorizeFilter`.
- Password hashing with ASP.NET Core's `PasswordHasher<T>` (PBKDF2 with a random salt).

## Tests (every behaviour has all three kinds)

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| Login | correct password → auth cookie set, profile opens | wrong password → "Wrong user name or password.", no cookie | no anti-forgery token → 400 |
| ReturnUrl after login | `/Admin` → redirected there | `https://evil.example.com/...` → ignored, goes to `/` | `javascript:alert(1)` → ignored, goes to `/` |
| Admin page | admin → "Admin area" | logged in as normal user → Access denied | not logged in → login page with `ReturnUrl=%2FAdmin` |
| Login required (global filter) | logged-in user → own profile | home page (`[AllowAnonymous]`) → open without login | forged auth cookie → treated as logged out |
| Register | new user → created; an extra `role=Admin` form field is **ignored** (still not an admin) | name `alice` taken → refused | empty name / empty or short password → refused |
| Logout | POST + token → logged out | `GET /Account/Logout` (e.g. an `<img>` on another site) → 405, still logged in | POST without token → 400, still logged in |

**How the tests stay isolated:** every test creates its own `HttpClient`, which has its own empty cookie jar,
so one test's login can never leak into another. The app runs in memory (`WebApplicationFactory<Program>`),
over `https://localhost` because the cookies are `Secure`. Users are kept in a static list for the whole run,
so the register test uses a random user name.

**Checked that the tests catch real bugs:** removing the `Url.IsLocalUrl` check makes both ReturnUrl negative
tests fail; removing `[Authorize(Roles = "Admin")]` makes the Admin-page and Register tests fail.

## Where the authorization boundaries are, and why

1. **Locked by default.** A global filter requires a login on *every* controller. Public pages must
   opt out with `[AllowAnonymous]`. If someone adds a new controller and forgets an attribute, it's
   *protected*, not exposed. Forgetting should fail safe.
2. **Admin rule on the class, not the action.** `[Authorize(Roles = "Admin")]` on `AdminController`
   covers every action added to it later.
3. **The server decides roles.** Registration always creates a `User`. The role comes from the user
   store and goes into the cookie when we sign in; there's no form field or URL that can set it.
4. **The cookie can't be forged.** It's encrypted and signed (Data Protection). Editing it to say
   `Role=Admin` makes it invalid, and the user is treated as logged out.
5. **Hiding a link is not security.** The nav bar shows "Admin" to everyone on purpose: the check is on
   the server, so knowing the URL doesn't help.

## Other security choices

- **Passwords are hashed**, never stored or compared as plain text.
- **Same error for wrong user and wrong password**, so attackers can't test which user names exist.
- **`Url.IsLocalUrl(returnUrl)`** stops "open redirect" attacks (`/Account/Login?returnUrl=https://evil.com`).
- **Logout is a POST with an anti-forgery token**, so another site can't log you out with an `<img>` tag.
- **Cookie:** `HttpOnly`, `Secure`, `SameSite=Lax`, 1-hour sliding expiry.
- **Real apps** should use ASP.NET Core Identity with a database (lockout after failed tries, email
  confirmation, 2FA). This project builds the core by hand to show how it works.

## Code snippets

**Cookie login, and "every page needs a login" by default**, from `CookieAuthAndRoles/Program.cs`

```csharp
builder.Services.AddControllersWithViews(options =>
{
    AuthorizationPolicy loggedInUsersOnly = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.Filters.Add(new AuthorizeFilter(loggedInUsersOnly));
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

app.UseAuthentication(); // who are you? (reads the cookie)
app.UseAuthorization();  // are you allowed? (checks [Authorize])
```

**Logging in: build claims, sign in, only follow local return URLs**, from `Controllers/AccountController.cs`

```csharp
var claims = new List<Claim>
{
    new Claim(ClaimTypes.Name, user.UserName),
    new Claim(ClaimTypes.Role, user.Role),
};
var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

if (Url.IsLocalUrl(returnUrl))
{
    return Redirect(returnUrl);
}
return RedirectToAction("Index", "Home");
```

**Logging out**

```csharp
await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
```

**Roles and anonymous pages**

```csharp
[Authorize(Roles = "Admin")] // on the class: every action is admin-only
public class AdminController : Controller

[AllowAnonymous]             // on the login page: must work when NOT logged in
public IActionResult Login(string? returnUrl)
```

**Hashing passwords (never store plain text)**, from `Stores/UserStore.cs`

```csharp
private static readonly PasswordHasher<AppUserModel> passwordHasher = new();

newUser.PasswordHash = passwordHasher.HashPassword(newUser, password);

PasswordVerificationResult result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
return result == PasswordVerificationResult.Failed ? null : user;
```

**Integration test: start the app in memory and check where it redirects**, from `CookieAuthAndRoles.Tests/`

```csharp
public class AuthorizationTest : IClassFixture<WebApplicationFactory<Program>>

HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
{
    BaseAddress = new Uri("https://localhost"),
    AllowAutoRedirect = false,
    HandleCookies = true,
});

HttpResponseMessage response = await client.GetAsync("/Admin");
Assert.Equal(302, (int)response.StatusCode);
```

The app's `Program.cs` ends with `public partial class Program { }` so `WebApplicationFactory<Program>` can find it.

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| `class AppUserModel` | `PasswordHash` is set *after* the object exists, because `PasswordHasher.HashPassword(user, ...)` needs the user. | A record created all at once can't do that order. |
| `static class UserStore` + `static readonly PasswordHasher<AppUserModel>` | One shared user list and one hasher (it holds no per-user state). | A real app uses ASP.NET Core Identity with a database. |
| `AppUserModel? CheckPassword` | Wrong password is a normal outcome → `null`. | An exception for a normal failed login is slow and noisy. |
| `bool Register` | `false` = name taken. | — |
| `List<Claim>` | The claims list given to `ClaimsIdentity`. | — |
| `string? returnUrl` | Usually missing. | — |
| `userName ?? ""` | If the form field is missing, compare against empty instead of crashing. | — |
| Global `AuthorizeFilter` + `[AllowAnonymous]` per action | Locked by default; each public page opts out. | `[Authorize]` per controller can be forgotten. |
