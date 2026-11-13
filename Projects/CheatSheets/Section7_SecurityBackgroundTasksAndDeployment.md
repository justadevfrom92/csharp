# Cheat Sheet — Section 7: Security, Background Tasks & Deployment

Projects: [7.1](../Section7_SecurityBackgroundTasksAndDeployment/Project7.1_CookieAuthAndRoles), [7.2](../Section7_SecurityBackgroundTasksAndDeployment/Project7.2_FormSecurityXssCsrf), [7.3](../Section7_SecurityBackgroundTasksAndDeployment/Project7.3_EnvironmentAwareEmail), [7.4](../Section7_SecurityBackgroundTasksAndDeployment/Project7.4_OutputCachingCatalog), [7.5](../Section7_SecurityBackgroundTasksAndDeployment/Project7.5_FeatureFlags), [7.6](../Section7_SecurityBackgroundTasksAndDeployment/Project7.6_GitHubActionsCi), [7.7](../Section7_SecurityBackgroundTasksAndDeployment/Project7.7_IdempotentMigrationScripts), [7.8](../Section7_SecurityBackgroundTasksAndDeployment/Project7.8_RawWebSockets), [7.9](../Section7_SecurityBackgroundTasksAndDeployment/Project7.9_RedisDistributedCache)

## Dependency injection lifetimes

```csharp
builder.Services.AddSingleton<PriceCache>();         // ONE for the whole app
builder.Services.AddScoped<OrderService>();          // one per HTTP request (DbContext is scoped)
builder.Services.AddTransient<EmailBuilder>();       // a NEW one every time it's asked for

builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();          // interface -> class
builder.Services.AddKeyedSingleton<IEmailSender, FileEmailSender>("file"); // several, by key
builder.Services.AddSingleton<IEmailSender>(serviceProvider =>           // build it yourself
    new LoggingEmailSender(serviceProvider.GetRequiredKeyedService<IEmailSender>("file")));

public PreviewController([FromKeyedServices("file")] IEmailSender sender)
```

Never inject a **scoped** service into a **singleton** (it would live forever). Use `IServiceScopeFactory` there:

```csharp
using IServiceScope scope = scopeFactory.CreateScope();
var database = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
```

## Cookie login and roles

```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });
app.UseAuthentication();
app.UseAuthorization();

var claims = new List<Claim> { new Claim(ClaimTypes.Name, userName), new Claim(ClaimTypes.Role, "Admin") };
await HttpContext.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
await HttpContext.SignOutAsync();

[Authorize]                    // must be logged in
[Authorize(Roles = "Admin")]   // must have the role
[AllowAnonymous]               // anyone

bool isAdmin = User.IsInRole("Admin");
string? name = User.Identity?.Name;
```

## Passwords

```csharp
var hasher = new PasswordHasher<AppUser>();
user.PasswordHash = hasher.HashPassword(user, password);
bool ok = hasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;
```

## CSRF and XSS

```csharp
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); // every POST checked
});
```

```cshtml
<p>@userText</p>               @* SAFE: encoded *@
@Html.Raw(userText)            @* DANGER with user input *@
```

```csharp
if (Url.IsLocalUrl(returnUrl))  // never redirect to someone else's site
{
    return Redirect(returnUrl);
}
```

## Middleware

```csharp
// inline
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Frame-Options"] = "DENY";
    await next(context);
});

// a class (convention style)
public class TimingMiddleware
{
    private readonly RequestDelegate next;

    public TimingMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var timer = Stopwatch.StartNew();
        await next(context);
        Console.WriteLine($"{context.Request.Path} {timer.ElapsedMilliseconds} ms");
    }
}
app.UseMiddleware<TimingMiddleware>();
```

Usual order: `UseExceptionHandler` → `UseHsts`/`UseHttpsRedirection` → `UseStaticFiles` → `UseRouting` → `UseAuthentication` → `UseAuthorization` → `UseOutputCache`/`UseRateLimiter` → `Map...`.

## Filters

```csharp
public class LogActionFilterAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // before the action; set context.Result to stop it
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        // after the action
    }
}
```

## Background work

```csharp
public class CleanupBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(10));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            // do the job
        }
    }
}
builder.Services.AddHostedService<CleanupBackgroundService>();
```

## Environments

```csharp
if (app.Environment.IsDevelopment()) { }
if (app.Environment.IsEnvironment("UAT")) { }
```

```bash
ASPNETCORE_ENVIRONMENT=Production dotnet run
```

`Properties/launchSettings.json` sets it for `dotnet run`; `appsettings.{Environment}.json` is loaded on top of `appsettings.json`.

## Output caching and health checks

```csharp
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("Catalog", policy => policy.Expire(TimeSpan.FromSeconds(60)).Tag("catalog"));
});
app.UseOutputCache();

[OutputCache(PolicyName = "Catalog")]
await outputCacheStore.EvictByTagAsync("catalog", cancellationToken);

builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");
app.MapHealthChecks("/health");
```

## Feature flags

```csharp
builder.Services.Configure<FeatureFlagSettings>(builder.Configuration.GetSection("FeatureFlags"));
bool on = flags.CurrentValue.TryGetValue("NewCheckout", out bool enabled) && enabled; // IOptionsMonitor
```

## Distributed cache (Redis)

```csharp
builder.Services.AddStackExchangeRedisCache(options => options.Configuration = "localhost:6379");

await cache.SetStringAsync("price:1", json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
string? cached = await cache.GetStringAsync("price:1");
await cache.RemoveAsync("price:1");
```

## WebSockets

```csharp
app.UseWebSockets();
app.Map("/ws", async context =>
{
    using WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
    var buffer = new byte[1024];
    WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, context.RequestAborted);
    await socket.SendAsync(buffer.AsMemory(0, result.Count), WebSocketMessageType.Text, true, context.RequestAborted);
});
```

## Migrations for production

```bash
dotnet ef migrations add AddOrders
dotnet ef migrations script --idempotent --output migrations.sql   # give this to the DBA / pipeline
dotnet ef migrations bundle --output efbundle                      # or a runnable file
```

## GitHub Actions (`.github/workflows/ci.yml`)

```yaml
name: CI
on: [push, pull_request]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - run: dotnet build -c Release
      - run: dotnet test -c Release --no-build
```
