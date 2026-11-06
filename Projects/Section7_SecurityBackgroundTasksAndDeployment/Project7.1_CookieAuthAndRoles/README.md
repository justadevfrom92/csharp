# Project 7.1 — Cookie Login & Role-Based Access

```bash
dotnet dev-certs https --trust
```

```bash
dotnet run
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

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| `class AppUser` | `PasswordHash` is set *after* the object exists, because `PasswordHasher.HashPassword(user, ...)` needs the user. | A record created all at once can't do that order. |
| `static class UserStore` + `static readonly PasswordHasher<AppUser>` | One shared user list and one hasher (it holds no per-user state). | A real app uses ASP.NET Core Identity with a database. |
| `AppUser? CheckPassword` | Wrong password is a normal outcome → `null`. | An exception for a normal failed login is slow and noisy. |
| `bool Register` | `false` = name taken. | — |
| `List<Claim>` | The claims list given to `ClaimsIdentity`. | — |
| `string? returnUrl` | Usually missing. | — |
| `userName ?? ""` | If the form field is missing, compare against empty instead of crashing. | — |
| Global `AuthorizeFilter` + `[AllowAnonymous]` per action | Locked by default; each public page opts out. | `[Authorize]` per controller can be forgotten. |
