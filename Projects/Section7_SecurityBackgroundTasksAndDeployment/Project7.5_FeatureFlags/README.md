# Project 7.5 — Feature Flags

```bash
dotnet run
```
Edit the `FeatureFlags` section in `appsettings.json` while the app runs, then refresh the page.

```json
"FeatureFlags": { "HolidayBanner": true, "NewCheckout": false, "ProductReviews": false }
```

## Three ways a flag is used

| Where | How | Example |
|---|---|---|
| Razor view | `@inject FeatureFlagService` (in `_ViewImports`) + `@if (FeatureFlags.IsEnabled(...))` | Holiday banner, Reviews link |
| Inside an action | `if (featureFlags.IsEnabled(...))` picks old or new code | `/Shop/Checkout` |
| Whole action | `[RequireFeatureFlag(FeatureNames.ProductReviews)]` returns 404 when off | `/Shop/Reviews` |

Override per environment without touching the file:

```bash
FeatureFlags__NewCheckout=true dotnet run
```

## Deployment strategy: deploy ≠ release

Without flags, *deploying* code and *releasing* a feature are the same moment: as soon as the new
checkout is on the server, every customer gets it, bugs and all. To undo it you must roll back the
whole deployment.

With flags they are separate steps:

1. **Merge and deploy early, flag OFF.** Half-finished work can go to production safely, hidden.
   No long-lived feature branches and no giant merge at the end.
2. **Test in production.** Turn the flag on in UAT, or on one server, and check it with real data.
3. **Release = change config.** Flip `NewCheckout` to `true`. `IOptionsMonitor` picks it up within
   seconds — no build, no deploy, no restart.
4. **Kill switch.** If error rates go up, flip it back to `false`. Recovery takes seconds instead of a
   rollback deployment.
5. **Clean up.** Once the feature is stable, delete the flag and the old code path. Old flags are
   technical debt: every flag doubles the number of ways the code can run.

## Why these choices

- **Missing flag = off.** A typo in config or a forgotten key hides a feature instead of exposing an
  unfinished one.
- **`FeatureNames` constants.** `IsEnabled("NewChekout")` would compile and silently be off forever;
  `FeatureNames.NewChekout` doesn't compile.
- **404 instead of 403 for disabled pages.** The feature should look like it doesn't exist yet.
- **`IOptionsMonitor` instead of `IOptions`.** `IOptions` reads config once at startup; the monitor
  sees changes live, which is the whole point of a flag.
- **Why not a library?** `Microsoft.FeatureManagement` adds percentages, user targeting and time windows.
  For on/off flags, ~40 lines of our own code is easier to understand. Switch when you need those extras.
- **Flags are not security.** A hidden link is still reachable if someone guesses the URL — that's why
  `Reviews` is also protected by the attribute on the server.

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| `static class FeatureNames` with `const string` | Attributes only accept compile-time constants: `[RequireFeatureFlag(FeatureNames.NewCheckout)]`. Typos become compile errors. | `static readonly` can't be used inside an attribute. |
| `FeatureFlagSettings : Dictionary<string, bool>` | Config binds any number of flag names into it. Its own type name makes it easy to register in DI. | A class with one property per flag needs a code change for every new flag. |
| `IOptionsMonitor<T>` | Always gives the latest config, so flags change without a restart. | `IOptions<T>` reads once at startup. |
| `FeatureFlagService` singleton class | One place for the "missing = off" rule. | Reading the dictionary directly everywhere repeats that rule. |
| `RequireFeatureFlagAttribute : ActionFilterAttribute` | MVC runs `OnActionExecuting` before the action; we only override that. | — |
| Service from `RequestServices` inside the attribute | Attributes are created by the compiler, not DI, so they can't take constructor services. | `[ServiceFilter]` is an alternative but needs extra registration. |
| `NotFoundResult` | A disabled page should look like it doesn't exist. | `403` admits the page is there. |
