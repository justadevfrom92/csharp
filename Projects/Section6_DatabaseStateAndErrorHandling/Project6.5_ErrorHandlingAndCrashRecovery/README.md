# Project 6.5 — Site Error Handling & Crash Recovery

> New to `.cshtml` files (`@model`, `@foreach`, `ViewBag`, layouts)? Read [RAZOR_BASICS.md](../../RAZOR_BASICS.md) first.

```bash
dotnet run --launch-profile Production
```

```bash
dotnet run --launch-profile Development
```

Open `http://localhost:5065` and click the links. Compare what the browser shows in each mode,
and what lands in the console and in `logs/errors-<date>.json`.

## What it covers

- `UseExceptionHandler("/Error/ServerError")` — catches any unhandled exception (Production).
- `UseDeveloperExceptionPage()` — full stack trace, Development only.
- `UseStatusCodePagesWithReExecute("/Error/Status/{0}")` — friendly 404 / other status pages.
- Custom views: `ServerError` (500), `NotFound` (404), `OtherStatus`.
- **Serilog** structured logging: console + JSON file (warnings and errors only), created before
  the app starts so startup crashes are logged too (`Log.Fatal` + `CloseAndFlush`).

## Exception sanitization — why the user never sees the real error

`/Home/Crash` throws an exception whose message contains a database user, password and server IP.
That's realistic: exception messages and stack traces often contain connection strings, SQL,
file paths, library versions, or other users' data.

If that text reached the browser, an attacker would learn exactly how to attack the system
(this is called **information disclosure**, OWASP "Security Misconfiguration").

So the app separates *who gets what*:

| Who | Gets | Where |
|---|---|---|
| User | Generic message + an **error id** | `ServerError.cshtml` |
| Developers / ops | Full exception, stack trace, path, same error id | Serilog log file |

The error id links the two: a user reports "error 00-4bf9...", support searches the logs for it.

**Why the Developer Exception Page is only for Development:** it shows the stack trace, source lines,
headers and cookies. Great for debugging on your machine, a disaster on a public server. The check is on
the environment name, so a production deployment can't turn it on by accident.

**Why status-code pages "re-execute" instead of redirect:** re-execute keeps the original URL and the
correct status code (404). A redirect would send the browser to `/Error` with status 200 (bad for search
engines and monitoring).

**Why the log file is JSON:** structured logs (`{ "ErrorId": "...", "Path": "/Home/Crash" }`) can be searched
and filtered by field in tools like Seq, Elastic or Azure, instead of grepping text.

**Why `UseExceptionHandler` first in the pipeline:** middleware only catches errors from things that run
after it, so it must wrap everything else.

## Code snippets

**Create the logger first, so even a startup crash is logged**, from `Program.cs`

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(new CompactJsonFormatter(), "logs/errors-.json", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();
    // ...
    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "The app failed to start");
}
finally
{
    Log.CloseAndFlush(); // write any logs still in memory before the process ends
}
```

**Developer error page vs friendly error page, plus nice 404 pages**

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // full stack trace: ONLY on your own machine
}
else
{
    app.UseExceptionHandler("/Error/ServerError");
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/Error/Status/{0}"); // e.g. /Error/Status/404
```

**The error page: full details to the log, only an id to the user**, from `Controllers/ErrorController.cs`

```csharp
[Route("/Error/ServerError")]
public IActionResult ServerError()
{
    IExceptionHandlerPathFeature? errorDetails = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
    string errorId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

    logger.LogError(errorDetails?.Error, "Unhandled error {ErrorId} on {Path}", errorId, errorDetails?.Path);

    Response.StatusCode = StatusCodes.Status500InternalServerError;
    ViewBag.ErrorId = errorId;
    return View();
}
```

**Structured logging: `{Name}` placeholders become searchable fields, not just text**

```csharp
logger.LogWarning("Status {StatusCode} for {Path}", statusCode, reExecuteDetails?.OriginalPath);
```

## Variations

The same crash (a message containing a database password) handled different ways, in `Variations/`
(`/variations/errors/...`). Results from `curl` with the app in **Production**:

```bash
ASPNETCORE_ENVIRONMENT=Production dotnet run --no-launch-profile
```

(`--no-launch-profile` matters: `dotnet run` otherwise applies `Properties/launchSettings.json`, which sets Development —
and then the developer error page shows the password and full stack trace. That happened while testing these.)

| Way | Response | Password shown? | The difference |
|---|---|---|---|
| `UseExceptionHandler("/Error/ServerError")` (original, `/Home/Crash`) | 500, HTML "Sorry, something went wrong" + error id | no | one place for the whole app |
| `IExceptionHandler` class (`ApiErrorExceptionHandlerVariation`, `/handler`) | 500, JSON `{"title":"Something went wrong.","errorId":"..."}` | no | .NET 8+; asked **before** the error page; can choose by URL (JSON for APIs, HTML for pages) |
| MVC exception filter (`[HideErrorsFilterVariation]`, `/filter`) | 500, JSON from the filter | no | only for the controllers/actions it's put on |
| `try/catch` in the action (`/try-catch`) | 503 "Please try again later." | no | full control, repeated in every action |
| `try/catch` returning `exception.Message` (`/leaky`) | 500 "...with password 'Hunter2'" | **yes** | the mistake sanitization exists to prevent |

**Gotcha found while writing `ApiErrorExceptionHandlerVariation`:** with `UseExceptionHandler("/Error/ServerError")`,
by the time an `IExceptionHandler` runs, `Request.Path` has **already been changed** to `/Error/ServerError`. The URL
that failed is in `HttpContext.Features.Get<IExceptionHandlerPathFeature>().Path`.

**Status-code pages** (one line in `Program.cs`, documented only):

| Line | `/does-not-exist` gives | The difference |
|---|---|---|
| `UseStatusCodePagesWithReExecute("/Error/Status/{0}")` (original) | 404 + the nice page, **URL unchanged** | correct status for browsers and search engines |
| `UseStatusCodePagesWithRedirects("/Error/Status/{0}")` | **302** → `/Error/Status/404` | the address bar changes and the first answer is a redirect, not 404 (this app's error page then sets 404 itself; a page that doesn't would return 200) |
| `UseStatusCodePages()` | 404 + plain text "Status Code: 404; Not Found" | no page to design; fine for APIs |

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| Static `Log.Logger` created before the host | Must exist *before* the app starts, so startup crashes are logged. | A logger from DI only exists after startup succeeded. |
| `try` / `catch` / `finally` around the whole app | `catch` logs a fatal startup error; `finally` flushes logs even when crashing. | Without `CloseAndFlush`, the last log lines (often the important ones) can be lost. |
| `IExceptionHandlerPathFeature?` | Only set when we arrived via the exception handler; could be missing if someone opens `/Error/ServerError` directly. | — |
| `Activity.Current?.Id ?? HttpContext.TraceIdentifier` | Use the trace id if tracing is on, otherwise the request id. `?.` and `??` handle "no current activity". | — |
| `{statusCode:int}` route + `int statusCode` | Only numbers match the route. | — |
| Separate `ErrorController` | Error pages are their own concern, reached only by middleware. | Mixing them into `HomeController` blurs that. |
