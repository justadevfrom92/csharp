# Project 7.2 — Form Security: XSS & CSRF

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
