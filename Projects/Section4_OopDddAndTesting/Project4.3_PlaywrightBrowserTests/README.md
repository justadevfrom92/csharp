# Project 4.3 — Playwright Browser Tests for the MVC Catalog

Real browser tests for **Project 3.1 (Product Catalog)**. They open pages, fill inputs, pick
from a `<select>`, click Save, then check the redirect URL, the success alert and the table.

## Run

First time only — download the browser Playwright uses:

```bash
dotnet build
```

```bash
pwsh bin/Debug/net10.0/playwright.ps1 install chromium
```

Then:

```bash
dotnet test
```

Already have Chromium installed? Skip the install and point at it instead:

```bash
CHROMIUM_PATH=/path/to/chrome dotnet test
```

## How it works

- `ProductCatalogWebsiteFixture` starts the real 3.1 app **inside the test process** with
  `WebApplicationFactory<Program>` + `UseKestrel(0)` (port 0 = pick any free port), and launches
  one headless Chromium.
- `ProductCatalogBrowserTest` drives the pages using element ids (`#Name`, `#save-button`,
  `#success-alert`) that the views render.

## Test isolation — how tests avoid affecting each other

1. **Fresh browser context per test.** `InitializeAsync` creates a new `BrowserContext` before
   each test: new cookies, new storage, new tab. One test's anti-forgery cookie or `TempData`
   can never leak into the next one. Contexts are cheap (milliseconds), unlike a whole new browser.
2. **One browser + one server per test class.** Starting Chromium and the website is slow, so they
   are shared through `IClassFixture` and closed once at the end.
3. **Random port.** `UseKestrel(0)` means tests never fail because port 5000 is in use, and two
   runs (e.g. on CI) can happen side by side.
4. **Unique test data.** The "add product" test uses a random name (`Lamp 3fa9c1`), so it never
   matches a product created by another test or run.
5. **Assertions don't depend on order.** Each test sets up what it needs and checks only its own
   data, so they pass in any order.

**Known limit:** the 3.1 app keeps products in a static in-memory list, so data *does* live for
the whole test class. With a database (Section 6) you'd reset or use a fresh database per test class.

## Why `WebApplicationFactory` instead of starting `dotnet run` in the background

No separate process to start, wait for and kill, no fixed port, and when the tests end the server
is guaranteed to stop. It's the same app code — just hosted by the test.

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| `class ProductCatalogWebsiteFixture : IAsyncLifetime` | xUnit calls `InitializeAsync`/`DisposeAsync` on this interface — needed because starting a browser is async. | A constructor can't `await`; `IDisposable` can't be async. |
| `IClassFixture<ProductCatalogWebsiteFixture>` | One server + one browser shared by all tests in the class. | Creating them per test would make every test seconds slower. |
| `IBrowser Browser { get; private set; } = null!` | Set in `InitializeAsync`, not the constructor. `null!` tells the compiler it will be set before use; `private set` stops tests from replacing it. | Making it nullable would force `Browser!` everywhere. |
| `WebApplicationFactory<Program>? factory` | Really can be null if setup failed, so `DisposeAsync` checks it. | — |
| `IReadOnlyList<string>` for error texts | That's what Playwright's `AllInnerTextsAsync` returns; we only read it. | — |
| `Guid` slice for product names | A unique name per run, so tests never match each other's data. | A fixed name breaks the second time the tests run against the same app. |
