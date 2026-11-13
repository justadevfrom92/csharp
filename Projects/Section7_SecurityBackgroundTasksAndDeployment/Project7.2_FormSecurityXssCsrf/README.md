# Project 7.2 — Form Security: XSS & CSRF

> New to `.cshtml` files (`@model`, `@foreach`, `ViewBag`, layouts)? Read [RAZOR_BASICS.md](../../RAZOR_BASICS.md) first.

A guestbook where anyone can post — the classic place for both attacks.

```bash
dotnet dev-certs https --trust
```

```bash
dotnet run
```
Open `https://localhost:7072` (HTTPS is required: the cookies are marked `Secure`).
Post `<script>alert('hacked')</script>` as a message: it appears as plain text and nothing runs.

## Threat model

| # | Threat | How the attack works | Mitigation in this project |
|---|---|---|---|
| 1 | **Stored XSS** | Attacker posts `<script>...</script>`. Every visitor's browser runs it, which can steal data or act as them. | Razor `@value` **HTML-encodes** output (`<` becomes `&lt;`). `Html.Raw` is never used on user input. |
| 2 | **XSS via links** | Website = `javascript:alert(1)`; code runs when someone clicks. | `[Url]` on the model plus `IsSafeWebLink` in the controller: only `http`/`https` links are accepted. |
| 3 | **XSS that slips through** (a future bug) | A developer adds `Html.Raw` somewhere by mistake. | **Content-Security-Policy** header: browser refuses inline scripts and scripts from other sites. A safety net, not the main defence. |
| 4 | **CSRF** | Evil site has a hidden form that POSTs to our site. The browser adds the victim's cookies automatically, so it looks like the victim did it. | **Anti-forgery token**: a secret value in a hidden field + a cookie. The evil site can't read our page, so it can't know the token. Enforced on *every* POST by the global `AutoValidateAntiforgeryToken` filter. |
| 5 | **CSRF (second layer)** | Same as above. | Anti-forgery cookie is `SameSite=Strict`: browsers don't send it with requests started from other sites. |
| 6 | **Clickjacking** | Our page is loaded invisibly in an `<iframe>` on another site to trick clicks. | CSP `frame-ancestors 'none'`. |
| 7 | **Content sniffing** | Browser guesses an uploaded text file is HTML and runs it. | `X-Content-Type-Options: nosniff`. |
| 8 | **Huge input** | Very long messages to fill memory/disk. | `[StringLength]` limits on every field. |

## The security-headers middleware class

`Middlewares/SecurityHeadersMiddleware.cs` adds `Content-Security-Policy`, `X-Content-Type-Options` and
`Referrer-Policy` to **every** response. It's registered in `Program.cs` with
`app.UseMiddleware<SecurityHeadersMiddleware>()`.

It's a **convention-based** middleware: no interface, ASP.NET Core recognises it by its shape — a constructor
taking `RequestDelegate next` and a `public Task InvokeAsync(HttpContext context)`. Inside, code before
`await next(context)` runs on the way **in**, code after it on the way **out**. It's created once at startup,
so it must not store per-request data in fields. (Project 7.4 also shows the other style, `IMiddleware`.)

**Why a class instead of an inline `app.Use(...)` lambda:** it has a name you can find, its own file, and
`Program.cs` stays a short list of steps.

## Why these choices

- **Encode on output, not on input.** We store exactly what was typed and encode when showing it.
  The right encoding depends on *where* it's shown (HTML, attribute, JavaScript, URL), which you only
  know at display time. Encoding on input also mangles data (`Tom & Jerry` → `Tom &amp; Jerry` in the database).
- **Global anti-forgery filter instead of `[ValidateAntiForgeryToken]` on each action.** A forgotten
  attribute means an open hole; a global rule can't be forgotten. GET requests aren't checked, which is
  why GETs must never change data.
- **`@Html.AntiForgeryToken()`** is what you'd write by hand in a plain HTML form; the `<form asp-action>`
  tag helper adds it for us.
- **CSP is the backup, not the plan.** Encoding stops XSS; CSP limits the damage if encoding is ever missed.

## Code snippets

**CSRF: check the anti-forgery token on every POST, for every controller**, from `Program.cs`

```csharp
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
```

**XSS: Razor encodes `@value` automatically; `Html.Raw` does not**, from `Views/Guestbook/Index.cshtml`

```cshtml
@* SAFE: "<script>" is shown as text, not run. *@
<p><b>@entry.Name</b> wrote: @entry.Message</p>

@* NEVER do this with user input:  @Html.Raw(entry.Message) *@
```

**Only allow `http://` and `https://` links (blocks `javascript:...`)**, from `Controllers/GuestbookController.cs`

```csharp
private static bool IsSafeWebLink(string link)
{
    return Uri.TryCreate(link, UriKind.Absolute, out Uri? uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

if (!string.IsNullOrEmpty(entry.Website) && !IsSafeWebLink(entry.Website))
{
    ModelState.AddModelError(nameof(entry.Website), "Only http:// and https:// links are allowed.");
}
```

**A custom middleware class that adds security headers**, from `Middlewares/SecurityHeadersMiddleware.cs`

```csharp
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; frame-ancestors 'none'";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        await next(context); // the rest of the pipeline
    }
}

app.UseMiddleware<SecurityHeadersMiddleware>(); // in Program.cs
```

**HTTPS everywhere**

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();
```

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| `class GuestbookEntryModel` (form model) | Model binding sets each field; validation attributes on the properties. | See 3.1. |
| `string? Website` | Optional field. | — |
| `DateTime PostedAtUtc` | Sort newest first; always UTC. | — |
| `static List<GuestbookEntryModel>` + `lock` | Shared in-memory store across requests. | Same pattern and trade-offs as 3.1. |
| `static bool IsSafeWebLink` | Pure check using only its input. | — |
| `Uri.TryCreate(..., out Uri? uri)` | Parses properly instead of string tricks like `StartsWith("http")`, which `httpjavascript:...` could fool. | — |
| `SecurityHeadersMiddleware` (convention-based class) | Same headers on every response, in one named, findable place. | An inline `app.Use(...)` lambda buries the rules in `Program.cs`. |
