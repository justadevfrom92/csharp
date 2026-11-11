# Project 8.2 — ASP.NET Core Identity + JWT + Policy Authorization

| Folder | What |
|---|---|
| `IdentityJwtPolicies/` | API: register, log in (get a JWT), and endpoints protected by policies |
| `IdentityJwtPolicies.Tests/` | 28 tests: register, login + lockout, token checks, every policy |

```bash
dotnet run --project IdentityJwtPolicies
```

```bash
curl -H "Content-Type: application/json" -d '{"email":"fin@example.com","password":"Finance1!x","department":"Finance","birthYear":1990}' localhost:5000/account/register
```

```bash
curl -H "Content-Type: application/json" -d '{"email":"fin@example.com","password":"Finance1!x"}' localhost:5000/account/login
```

```bash
curl -H "Authorization: Bearer <accessToken from the login answer>" localhost:5000/reports/finance
```

```bash
dotnet test IdentityJwtPolicies.Tests
```

A seeded admin exists: `admin@example.com` / `Admin123!x`.

## How this differs from Project 7.1

| | 7.1 Cookie login | 8.2 Identity + JWT |
|---|---|---|
| Users stored | hand-written in-memory list | **ASP.NET Core Identity** in a database (SQLite here) |
| Password rules, hashing, lockout | hashing only | all built in (8+ chars, digit, upper-case, symbol; **locked after 3 wrong tries**) |
| Proof of login | encrypted **cookie** (browser sends it automatically) | **JWT** in the `Authorization: Bearer` header (the client sends it on purpose) |
| Best for | websites with pages | APIs called by mobile apps, SPAs, other servers |
| Authorization | roles only | **policies**: role, claim, and custom requirements |

## The pieces

**Identity** (`AddIdentityCore` + `AddEntityFrameworkStores`): `UserManager` creates users and stores roles and
claims; `SignInManager.CheckPasswordSignInAsync(..., lockoutOnFailure: true)` checks passwords and counts failures.
`IdentityDbContext` already defines all the tables.

**JWT** (`Services/JwtTokenService.cs`): a token is `header.payload.signature`. The payload holds **claims**
(user id, email, roles, department, birth year), the issuer, the audience and the expiry. The **payload is
readable by anyone** (it's only base64) — never put secrets in it. The **signature** (HMAC-SHA256 with the
signing key) proves the server made it and nobody changed it.

**Checking incoming tokens** (`AddJwtBearer`): every request's token must pass **all** of: right issuer, right
audience, valid signature, not expired (30 s clock skew allowed). Any failure → **401**.

**Policies** (`AddAuthorizationBuilder().AddPolicy(...)`):

| Policy | Kind | Rule | Used on |
|---|---|---|---|
| `AdminOnly` | role | `RequireRole("Admin")` | `/admin/dashboard` |
| `FinanceDepartment` | claim | `RequireClaim("department", "finance")` | `/reports/finance` |
| `AtLeast18` | custom | `MinimumAgeRequirement(18)` checked by `MinimumAgeAuthorizationHandler` using the `birth_year` claim | `/content/adults-only` |

**401 vs 403:** **401 Unauthorized** = "I don't know who you are" (no token, bad token). **403 Forbidden** = "I know
who you are, but you're not allowed".

## Why these choices

- **Policies instead of `if (user.IsInRole(...))` in endpoints:** the rule has a name, lives in one place, and
  endpoints only say `.RequireAuthorization("FinanceDepartment")`. Change the rule once, every endpoint follows.
- **A custom requirement + handler for age:** some rules aren't "has role X" or "has claim Y" — they need code.
  The handler gets `TimeProvider` injected, so tests could even fake the date.
- **Missing claim = no:** the admin has no `birth_year`, so `AtLeast18` refuses them. A check that can't be made
  must fail closed.
- **Role never comes from the request:** registration always adds the `User` role.
- **Same message for unknown email and wrong password,** so nobody can test which emails have accounts.
- **Lockout** stops password guessing: 3 wrong tries locks the account for 5 minutes (`423 Locked`).
- **Signing key in appsettings is a demo value.** In real apps it comes from a secret store / environment
  variable and is at least 32 random bytes.

## Tests (every behaviour has all three kinds)

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| Register | valid → 201, can log in | too short / no upper-case / no digit / no symbol → 400 naming the rule; email taken → 400 | not an email, empty email, empty password → 400 |
| Login | right password → 3-part JWT that opens `/me` | wrong password → 401; **3 wrong → 423 even with the right password** | unknown email → 401 with the **same** message; empty fields → 400 |
| Token | token from login → accepted | correctly signed but **expired**, or for **another audience** → 401 | signed with another key, **tampered** payload claiming Admin, garbage, none → 401 |
| AdminOnly | admin → 200 | normal user → 403 | no token → 401 |
| FinanceDepartment | "Finance" user → 200 | "sales" user → 403 | no token → 401 |
| AtLeast18 | exactly 18 → 200 | 17 → 403 | no `birth_year` claim → 403 |

Every test gets its own app and its own SQLite file (deleted afterwards), so users and lockouts never leak
between tests. Checked: with `ValidateLifetime = false`, the expired-token test fails.
