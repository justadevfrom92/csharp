# Cheat Sheet — Section 8: Beyond the Roadmap

Projects: [8.1](../Section8_BeyondTheRoadmap/Project8.1_MinimalApiOpenApi), [8.2](../Section8_BeyondTheRoadmap/Project8.2_IdentityJwtPolicies), [8.3](../Section8_BeyondTheRoadmap/Project8.3_HttpClientResilience), [8.4](../Section8_BeyondTheRoadmap/Project8.4_RateLimiting), [8.5](../Section8_BeyondTheRoadmap/Project8.5_DotnetPublish), [8.6](../Section8_BeyondTheRoadmap/Project8.6_RazorPages), [8.7](../Section8_BeyondTheRoadmap/Project8.7_Blazor), [8.8](../Section8_BeyondTheRoadmap/Project8.8_SignalR), [8.9](../Section8_BeyondTheRoadmap/Project8.9_Grpc)

## Minimal APIs

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
var app = builder.Build();
app.MapOpenApi();                                   // /openapi/v1.json

app.MapGet("/hello/{name}", (string name) => $"Hello {name}");
app.MapGet("/books/{id:int}", Results<Ok<BookModel>, NotFound> (int id, BookStore store) =>
{
    BookModel? book = store.Find(id);
    return book == null ? TypedResults.NotFound() : TypedResults.Ok(book);
});
app.MapPost("/books", (SaveBookRequest request, BookStore store) =>
{
    BookModel book = store.Add(request);
    return TypedResults.Created($"/books/{book.Id}", book);
});
app.MapDelete("/books/{id:int}", (int id, BookStore store) => store.Delete(id) ? Results.NoContent() : Results.NotFound());

RouteGroupBuilder api = app.MapGroup("/api").RequireAuthorization();  // shared prefix and rules
api.MapGet("/me", (ClaimsPrincipal user) => user.Identity!.Name);
app.Run();
```

## Endpoint filter

```csharp
app.MapPost("/books", CreateBook).AddEndpointFilter(async (context, next) =>
{
    if (context.HttpContext.Request.Headers["X-Api-Key"] != "secret")
    {
        return Results.Unauthorized();
    }
    return await next(context);
});
```

## JWT bearer tokens

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = "my-app",
            ValidAudience = "my-app-clients",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
        };
    });

var descriptor = new SecurityTokenDescriptor
{
    Issuer = "my-app",
    Audience = "my-app-clients",
    Subject = new ClaimsIdentity(claims),
    Expires = DateTime.UtcNow.AddMinutes(30),
    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256),
};
string token = new JsonWebTokenHandler().CreateToken(descriptor);
```

```bash
curl http://localhost:5000/me -H "Authorization: Bearer <token>"
```

## Authorization policies

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
    .AddPolicy("Finance", policy => policy.RequireClaim("department", "finance"))
    .AddPolicy("Adult", policy => policy.AddRequirements(new MinimumAgeRequirement(18)));

app.MapGet("/admin", () => "secret").RequireAuthorization("AdminOnly");
[Authorize(Policy = "Finance")]
```

## Identity

```csharp
builder.Services.AddIdentityCore<IdentityUser>(options => options.Password.RequiredLength = 8)
    .AddRoles<IdentityRole>()
    .AddSignInManager()
    .AddEntityFrameworkStores<AppIdentityDbContext>();

IdentityResult created = await userManager.CreateAsync(new IdentityUser { UserName = email, Email = email }, password);
await userManager.AddToRoleAsync(user, "User");
SignInResult result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
```

## HttpClient the right way

```csharp
builder.Services.AddHttpClient<WeatherClient>(client => client.BaseAddress = new Uri("https://api.example.com/"))
    .AddStandardResilienceHandler();                 // retry, timeout, circuit breaker with good defaults

public class WeatherClient
{
    private readonly HttpClient httpClient;

    public WeatherClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public Task<WeatherModel?> GetAsync(string city, CancellationToken cancellationToken)
    {
        return httpClient.GetFromJsonAsync<WeatherModel>($"weather/{city}", cancellationToken);
    }
}

HttpResponseMessage response = await httpClient.PostAsJsonAsync("orders", order);
response.EnsureSuccessStatusCode();
```

Never `new HttpClient()` per request (runs out of sockets).

## Rate limiting

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("fixed", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromSeconds(10);
    });
});
app.UseRateLimiter();
app.MapGet("/api/data", () => "ok").RequireRateLimiting("fixed");
```

Others: `AddSlidingWindowLimiter`, `AddTokenBucketLimiter`, `AddConcurrencyLimiter`.

## Razor Pages

```csharp
builder.Services.AddRazorPages();
app.MapRazorPages();

public class CreatePageModel : PageModel
{
    [BindProperty]
    public ContactModel Contact { get; set; } = new();

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        return RedirectToPage("./Index");
    }
}
```

```cshtml
@page "{id:int?}"
@model CreatePageModel
<form method="post"><input asp-for="Contact.Name" /></form>
<a asp-page="/Contacts/Edit" asp-route-id="1">Edit</a>
```

## Blazor

```razor
@page "/counter"
@rendermode InteractiveServer

<p>Count: @count</p>
<button @onclick="Increment">+1</button>
<input @bind="name" />
<ChildComponent Title="Hi" OnSaved="HandleSaved" />

@code {
    private int count;
    private string name = "";

    private void Increment()
    {
        count++;
    }

    private void HandleSaved(string value)
    {
    }
}
```

```razor
@code {
    [Parameter]
    public string Title { get; set; } = "";

    [Parameter]
    public EventCallback<string> OnSaved { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // load data when the component starts
    }
}
```

## SignalR

```csharp
builder.Services.AddSignalR();
app.MapHub<ChatHub>("/hubs/chat");

public class ChatHub : Hub
{
    public async Task SendMessage(string user, string text)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, text);
    }
}

await hubContext.Clients.Group("room1").SendAsync("ReceiveMessage", "system", "hi"); // from outside, IHubContext<ChatHub>
```

```js
const connection = new signalR.HubConnectionBuilder().withUrl("/hubs/chat").withAutomaticReconnect().build();
connection.on("ReceiveMessage", (user, text) => console.log(user, text));
await connection.start();
await connection.invoke("SendMessage", "Ann", "hello");
```

## gRPC

```proto
syntax = "proto3";
service Greeter {
  rpc SayHello (HelloRequest) returns (HelloReply);
}
message HelloRequest { string name = 1; }
message HelloReply { string message = 1; }
```

```xml
<Protobuf Include="Protos/greet.proto" GrpcServices="Server" />  <!-- or Client / Both -->
```

```csharp
builder.Services.AddGrpc();
app.MapGrpcService<GreeterService>();

public class GreeterService : Greeter.GreeterBase
{
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HelloReply { Message = "Hello " + request.Name });
    }
}

using GrpcChannel channel = GrpcChannel.ForAddress("https://localhost:5001");
var client = new Greeter.GreeterClient(channel);
HelloReply reply = await client.SayHelloAsync(new HelloRequest { Name = "Ann" });
```

## Publish

```bash
dotnet publish -c Release                                               # needs .NET on the server
dotnet publish -c Release -r linux-x64 --self-contained                 # includes .NET
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
dotnet publish -c Release -r linux-x64 -p:PublishAot=true               # native, fast start
```
