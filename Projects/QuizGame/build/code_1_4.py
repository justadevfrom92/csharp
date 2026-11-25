# Code shown with each question that has none in its definition. Key = question id (s<section>-<number>).
CODE = {}
def c(qid, code): CODE[qid] = code.strip("\n")

# ---------- Section 1 ----------
c("s1-001", r'''
decimal loanAmount = 20_000m;     // money: decimal
double distanceKm = 12.7;          // measurements: double
int numberOfMonths = 12;           // counts: int
decimal monthly = loanAmount / numberOfMonths;
''')
c("s1-005", r'''
int biggestInt = int.MaxValue;
try
{
    int result = checked(biggestInt + 1);
}
catch (OverflowException)
{
    Console.WriteLine("caught the problem");
}
''')
c("s1-007", r'''
string text = Console.ReadLine() ?? "";
int a = int.Parse(text);                 // "abc" -> FormatException
int b = Convert.ToInt32(text);           // "abc" -> FormatException
bool ok = int.TryParse(text, out int c); // "abc" -> false, c = 0
''')
c("s1-009", r'''
DateOnly firstPayment = new DateOnly(2027, 1, 15);
TimeOnly arrival = new TimeOnly(9, 30);
DateTime both = new DateTime(2027, 1, 15);   // 2027-01-15 00:00:00
Console.WriteLine(firstPayment);              // 15/01/2027
''')
c("s1-010", r'''
string? nickname = null;
string shown = nickname ?? "Guest";
Console.WriteLine(shown);
''')
c("s1-013", r'''
// A
switch (code)
{
    case 1: return "one";
    default: return "other";
}

// B
string label = code switch
{
    1 => "one",
    _ => "other",
};
''')
c("s1-016", r'''
string band = creditScore switch
{
    >= 800 => "Excellent",
    >= 700 and < 800 => "Good",
    >= 600 and < 700 => "Fair",
    _ => "Poor",
};
''')
c("s1-017", r'''
enum Status { Pending, Paid, Shipped }

string text = status switch
{
    Status.Pending => "waiting",
    Status.Paid => "paid",
    // Status.Shipped is missing
};
''')
c("s1-018", r'''
public record PaymentModel(decimal Amount);
public record CardPaymentModel(decimal Amount, string Last4) : PaymentModel(Amount);
public record GiftCardPaymentModel(decimal Amount, string Last4) : CardPaymentModel(Amount, Last4);

PaymentModel payment = new GiftCardPaymentModel(10m, "4242");
bool exact = payment.GetType() == typeof(CardPaymentModel);
bool isCard = payment is CardPaymentModel;
''')
c("s1-020", r'''
public record ParcelModel(decimal Weight, string Country);

decimal cost = parcel switch
{
    { Weight: > 20 } => 40m,
    { Country: "US" } => 5m,
    _ => 25m,
};
''')
c("s1-021", r'''
public static (decimal Total, int Count) Summarize(IReadOnlyList<decimal> prices)
{
    return (prices.Sum(), prices.Count);
}

var result = Summarize([19.99m, 5.01m]);
Console.WriteLine(result.Total);
''')
c("s1-024", r'''
(string Name, int Quantity) item = ("Tea", 12);
string json = JsonSerializer.Serialize(item);
Console.WriteLine(json);
''')
c("s1-026", r'''
public class ParcelModel
{
    public decimal WeightKg { get; }
    public string Destination { get; }

    public void Deconstruct(out decimal weightKg, out string destination)
    {
        weightKg = WeightKg;
        destination = Destination;
    }
}

var (weight, destination) = parcel;
''')
c("s1-027", r'''
(int X, int Y) point = (1, 2);
(int X, int Y) copy = point;
copy.X = 99;
Console.WriteLine($"{point.X} {copy.X}");
''')
c("s1-030", r'''
public record PersonModel(string Name)
{
    public List<string> Orders { get; init; } = [];
}

var buyer = new PersonModel("Ola") { Orders = ["Coffee"] };
var copy = buyer with { Name = "Kim" };
copy.Orders.Add("Tea");
Console.WriteLine(string.Join(", ", buyer.Orders));
''')
c("s1-031", r'''
public record PersonModel(string Name)
{
    public List<string> Orders { get; init; } = [];
}

var first = new PersonModel("Anna");
var second = new PersonModel("Anna");
Console.WriteLine(first == second);
''')
c("s1-032", r'''
public abstract record TransportationModel;
public sealed record PlaneModel(string FlightNumber, TimeOnly ArrivalTime) : TransportationModel;

// in another file:
public record FasterPlane(string FlightNumber, TimeOnly ArrivalTime)
    : PlaneModel(FlightNumber, ArrivalTime);
''')
c("s1-034", r'''
// old style: methods only
public static string ToLabelClassic(this TransportationModel transport) { ... }

// .NET 10
extension(TransportationModel transport)
{
    public string ToLabel() { ... }
    public TimeOnly ArrivesAt { get { ... } }
}
''')
c("s1-035", r'''
public abstract record TransportationModel;
public sealed record PlaneModel(string FlightNumber) : TransportationModel;
public sealed record CarModel() : TransportationModel;

string label = transport switch
{
    PlaneModel plane => $"flight {plane.FlightNumber}",
    CarModel => "car",
    _ => throw new ArgumentException("Unknown"),
};
''')
c("s1-036", r'''
public class PlainPerson
{
    public string Name { get; set; } = "";
}

var person = new PlainPerson { Name = "Anna" };
var copy = person with { Name = "Kim" };
''')
c("s1-037", r'''
public record UncheckedPersonVariation(string Name)
{
    public string Name { get; init; } =
        string.IsNullOrWhiteSpace(Name) ? throw new ArgumentException("Name is required.") : Name;
}

var valid = new UncheckedPersonVariation("Anna");
var blank = valid with { Name = "" };
''')
c("s1-044", r'''
string text = "";
foreach (string item in items)
{
    text += item + ",";
}

var builder = new StringBuilder();
foreach (string item in items)
{
    builder.Append(item).Append(',');
}
''')
c("s1-048", r'''
if (customer is not null)
{
}

if (customer != null)   // calls customer's == / != operator if it has one
{
}
''')

# ---------- Section 2 ----------
c("s2-001", r'''
List<Product> list = LoadProducts();
Product? a = list.Find(p => p.Code == "SKU-42");   // checks one by one

Dictionary<string, Product> byCode = list.ToDictionary(p => p.Code);
Product b = byCode["SKU-42"];                       // jumps straight to it
''')
c("s2-002", r'''
var tags = new HashSet<string>();
Console.WriteLine(tags.Add("tea"));   // True
Console.WriteLine(tags.Add("tea"));   // False
Console.WriteLine(tags.Count);        // 1
''')
c("s2-005", r'''
IEnumerable<Product> outOfStock = products.Where(p => p.QuantityInStock == 0); // (1)
foreach (Product product in outOfStock)                                          // (2)
{
}
List<Product> snapshot = outOfStock.ToList();                                    // (3)
''')
c("s2-006", r'''
var products = new[] { new Product("Tea", "Drinks"), new Product("Coffee", "Drinks") };

Product first = products.First(p => p.Category == "Drinks");
Product single = products.Single(p => p.Category == "Drinks");
''')
c("s2-008", r'''
bool a = products.Count() > 0;
bool b = products.Any();
''')
c("s2-011", r'''
var valueByCategory = products
    .GroupBy(product => product.Category)
    .Select(g => new { Category = g.Key, Value = g.Sum(p => p.Price * p.Quantity) });
''')
c("s2-012", r'''
var prices = new List<int> { 3, 1, 2 };
var sorted = prices.OrderBy(p => p).ToList();
Console.WriteLine(string.Join(",", prices));
''')
c("s2-013", r'''
Dictionary<string, int> dictionary = Load();
FrozenDictionary<string, int> frozen = dictionary.ToFrozenDictionary();
int stock = frozen["SKU-500"];
// frozen.Add(...) doesn't exist
''')
c("s2-014", r'''
public static IEnumerable<string> Generate(int count)
{
    for (int number = 1; number <= count; number++)
    {
        yield return $"ORD-{number:D4}";
    }
}
''')
c("s2-017", r'''
public static IEnumerable<string> Generate(int count)
{
    ArgumentOutOfRangeException.ThrowIfNegative(count);
    for (int number = 1; number <= count; number++)
    {
        yield return $"ORD-{number:D4}";
    }
}

IEnumerable<string> orders = Generate(-1);   // line A
string first = orders.First();              // line B
''')
c("s2-018", r'''
public static IEnumerable<string> ReadErrors(string path)
{
    using StreamReader reader = File.OpenText(path);
    string? line;
    while ((line = reader.ReadLine()) != null)
    {
        yield return line;
    }
}

foreach (string error in ReadErrors("app.log"))
{
    break;
}
''')
c("s2-020", r'''
IEnumerator<string> cursor = shelf.GetEnumerator();
while (cursor.????())
{
    string item = cursor.????;
}
''')
c("s2-021", r'''
var items = new List<string> { "Tea" };
foreach (string item in items)
{
    items.Add("Milk");
}
''')
c("s2-022", r'''
IQueryable<Order> bigOrders = database.Orders.Where(o => o.Total > 100);
int count = bigOrders.Count();
Order first = bigOrders.First();
''')
c("s2-024", r'''
public class ShelfCollection : IEnumerable<string>
{
    private readonly List<string> items = new();
    public void Add(string item) { items.Add(item); }
    public IEnumerator<string> GetEnumerator() { return items.GetEnumerator(); }
    IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
}

var shelf = new ShelfCollection { "Tea", "Coffee" };
''')
c("s2-031", r'''
string[] words = ["tea", "coffee", "tea"];
List<string> a = words.Distinct().ToList();
var b = new HashSet<string>(words);
bool hasTea = b.Contains("tea");
''')
c("s2-033", r'''
var lines = orders.Join(
    products,
    order => order.ProductCode,
    product => product.Code,
    (order, product) => new { order.Id, product.Name, order.Quantity });
''')
c("s2-034", r'''
public class Warehouse
{
    private readonly List<string> items = ["Tea"];
    public IReadOnlyList<string> Items => items;
    public void Restock(string item) { items.Add(item); }
}
''')

# ---------- Section 3 ----------
c("s3-001", r'''
public class ProductsController : Controller
{
    public IActionResult Index()
    {
        List<ProductModel> products = store.GetAll();
        return View(products);
    }
}
''')
c("s3-003", r'''
[HttpPost]
public IActionResult Create(ProductForm form)
{
    store.Add(form);
    return RedirectToAction("Index");
}
''')
c("s3-004", r'''
[HttpPost]
public IActionResult Create(ProductForm form)
{
    store.Add(form);
    TempData["Message"] = "Product added";
    ViewBag.Message = "Product added";
    return RedirectToAction(nameof(Index));
}
''')
c("s3-005", r'''
ViewBag.Title = "Products";
Console.WriteLine(ViewData["Title"]);
''')
c("s3-007", r'''
[HttpPost]
public IActionResult Create(ProductForm form)
{
    store.Add(form);
    return View("Index", store.GetAll());     // A
    // return RedirectToAction(nameof(Index)); // B
}
''')
c("s3-008", r'''
public class ProductForm
{
    [????]
    [StringLength(100)]
    public string Name { get; set; } = "";

    [Range(0.01, 10_000)]
    public decimal Price { get; set; }
}
''')
c("s3-009", r'''
public class ProductsController : Controller
{
    public IActionResult Index() { ... }

    [HttpPost]
    public IActionResult Create(ProductForm form)
    {
        return RedirectToAction("Indx");
    }
}
''')
c("s3-011", r'''
@model ProductModel

<h1>@Model.Name</h1>
<p>@Model.Price.ToString("C")</p>
''')
c("s3-012", r'''
@* product.Name is "<script>alert('hi')</script>" *@
<h2>@product.Name</h2>
<h2>@Html.Raw(product.Name)</h2>
''')
c("s3-013", r'''
public class RecentActivitySidebarViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(int maxItems)
    {
        var newest = activity.GetNewest(maxItems);
        return View(newest);
    }
}
''')
c("s3-015", r'''
public class RecentActivitySidebarViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View(entries);   // which .cshtml?
    }
}
''')
c("s3-016", r'''
public class IsoDateModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        string? text = context.ValueProvider.GetValue(context.ModelName).FirstValue;
        if (DateOnly.TryParseExact(text, "yyyy-MM-dd", out DateOnly date))
        {
            context.Result = ModelBindingResult.Success(date);
        }
        return Task.CompletedTask;
    }
}
''')
c("s3-017", r'''
@* _CartSummary.cshtml (a partial) *@
@model CartModel
<span>@Model.Items.Count items</span>

@* used as: *@
<partial name="_CartSummary" model="cart" />
''')
c("s3-018", r'''
[HttpPost]
public async Task<IActionResult> Upload(AvatarUploadForm form)
{
    ???? avatar = form.Avatar;
    await using Stream stream = avatar.OpenReadStream();
}
''')
c("s3-019", r'''
string uploadedName = form.Avatar.FileName;          // "cat.jpg"
string extension = Path.GetExtension(uploadedName);  // ".jpg"
byte[] firstBytes = new byte[4];
await form.Avatar.OpenReadStream().ReadExactlyAsync(firstBytes);
bool reallyJpeg = firstBytes[0] == 0xFF && firstBytes[1] == 0xD8;
''')
c("s3-021", r'''
[HttpPost]
[RequestSizeLimit(1_000_000)]
public IActionResult Upload(IFormFile avatar)
{
    // a 5 MB file was posted
    long size = avatar.Length;
}
''')
c("s3-022", r'''
string safeName = Path.GetRandomFileName() + ".jpg";
string path = Path.Combine(uploadsFolder, safeName);
// NOT: Path.Combine(uploadsFolder, form.Avatar.FileName)  -> "../../appsettings.json"
''')
c("s3-023", r'''
<form method="????" asp-action="Delete" asp-route-id="@product.Id">
    <button type="submit">Delete</button>
</form>
''')
c("s3-024", r'''
[HttpPost("products/{id:int}/edit")]
public IActionResult Edit(int id, ProductForm form)
{
}
// POST /products/5/edit?id=9  with form field id=7
''')
c("s3-025", r'''
<!DOCTYPE html>
<html>
<body>
    <header>Product Catalog</header>
    <main>@RenderBody()</main>
    <footer>© 2026</footer>
</body>
</html>
''')
c("s3-027", r'''
@using ProductCatalog
@using ProductCatalog.Models
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
''')
c("s3-028", r'''
TempData["Message"] = "Saved";          // request 1
string? first = TempData["Message"] as string;   // request 2: "Saved"
string? second = TempData["Message"] as string;  // request 3
''')
c("s3-030", r'''
<form method="post" asp-action="Create">
    <input name="Name" />
    <input name="Price" />
</form>

[HttpPost]
public IActionResult Create(ProductForm form) { ... }
''')

# ---------- Section 4 ----------
c("s4-001", r'''
dotnet new classlib -n SimpleHelpers
cd SimpleHelpers
dotnet ???? -c Release -o ../LocalNuGetFeed
''')
c("s4-002", r'''
<ItemGroup>
  <ProjectReference Include="../SimpleHelpers/SimpleHelpers.csproj" />
  <PackageReference Include="Roadmap.SimpleHelpers" Version="2.0.0" />
</ItemGroup>
''')
c("s4-004", r'''
<PackageReference Include="Roadmap.SimpleHelpers" Version="[2.0.0]" />
<PackageReference Include="Other.Package" Version="2.0.0" />
<PackageReference Include="Third.Package" Version="[2.0.0,3.0.0)" />
''')
c("s4-006", r'''
public record MoneyValueObject(decimal Amount, string Currency);

var a = new MoneyValueObject(10m, "USD");
var b = new MoneyValueObject(10m, "USD");
Console.WriteLine(a == b);   // True
''')
c("s4-007", r'''
public class BankAccountAggregate
{
    public decimal Balance { get; private set; }

    public void Withdraw(decimal amount)
    {
        if (amount > Balance)
        {
            throw new InsufficientFundsException(Balance, amount);
        }
        Balance -= amount;
    }
}
''')
c("s4-008", r'''
public record AccountOpenedEvent(Guid AccountId, DateTimeOffset OpenedAt);
public record MoneyWithdrawnEvent(Guid AccountId, decimal Amount);
''')
c("s4-009", r'''
public class BankAccountAggregate
{
    private BankAccountAggregate() { }

    public static BankAccountAggregate Open(string owner, decimal firstDeposit)
    {
        if (firstDeposit < 10m)
        {
            throw new ArgumentOutOfRangeException(nameof(firstDeposit));
        }
        return new BankAccountAggregate { ... };
    }
}
''')
c("s4-010", r'''
public record MoneyPositionalVariation(decimal Amount, string Currency)
{
    public decimal Amount { get; init; } =
        Amount >= 0 ? Amount : throw new ArgumentOutOfRangeException(nameof(Amount));
}

var positional = new MoneyPositionalVariation(10m, "USD");
var negative = positional with { Amount = -5 };
''')
c("s4-011", r'''
public class AccountRepositoryFake : IBankAccountRepository
{
    private readonly List<BankAccountAggregate> accounts = [];

    public void Save(BankAccountAggregate account) { accounts.Add(account); }
    public BankAccountAggregate? Find(Guid id) { return accounts.Find(a => a.Id == id); }
}
''')
c("s4-012", r'''
public class BankAccountTest
{
    [????]
    public void Withdraw_PositivePositive_EnoughMoney_LowersBalance()
    {
        var account = BankAccountAggregate.Open("Ann", 100m);
        account.Withdraw(30m);
        Assert.Equal(70m, account.Balance);
    }
}
''')
c("s4-015", r'''
builder.Services.AddSingleton<ProductStore>();
builder.Services.AddScoped<BankAccountService>();
builder.Services.AddTransient<IClock, SystemClock>();
''')
c("s4-016", r'''
await page.GotoAsync(fixture.BaseUrl + "/products");
await page.GetByRole(AriaRole.Link, new() { Name = "Add product" }).ClickAsync();
await page.GetByLabel("Name").FillAsync("Desk");
await Expect(page.GetByRole(AriaRole.Heading)).ToHaveTextAsync("Products");
''')
c("s4-018", r'''
page.Locator("div:nth-child(3) > button");
page.Locator("//div[2]/form/button[1]");
page.GetByRole(AriaRole.Button, new() { Name = "Save" });
''')
c("s4-019", r'''
public class ProductsTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public ProductsTest(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }
}
''')
c("s4-020", r'''
[Fact] public void Withdraw_PositivePositive_EnoughMoney_LowersBalance() { }
[Fact] public void Withdraw_PositiveNegative_ExactBalance_LeavesZero() { }
[Fact] public void Withdraw_NegativeNegative_TooMuch_Throws() { }
''')
c("s4-022", r'''
Banking.Domain/        <- rules only: no EF Core, no ASP.NET, no packages
  Aggregates/BankAccountAggregate.cs
  Repositories/IBankAccountRepository.cs
Banking.Demo/          <- wires it up
Banking.Tests/         <- tests the rules with fakes
''')
c("s4-023", r'''
<ItemGroup>
  <Reference Include="SimpleHelpers">
    <HintPath>libs/SimpleHelpers.dll</HintPath>
  </Reference>
</ItemGroup>
''')
c("s4-024", r'''
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <PackageId>Roadmap.SimpleHelpers</PackageId>
    <Version>2.0.0</Version>
    <Authors>Roadmap</Authors>
  </PropertyGroup>
</Project>
''')
c("s4-025", r'''
public class BankAccountAggregate
{
    public decimal Balance { get; private set; }

    public void Deposit(decimal amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        Balance += amount;
    }
}
''')
c("s4-026", r'''
public interface INotifier
{
    Task SendAsync(string message);
}

public abstract class BaseJob
{
    protected readonly ILogger logger;
    public abstract Task RunAsync();
    protected void Log(string text) { logger.LogInformation(text); }
}
''')
c("s4-029", r'''
public class Point
{
    public int X { get; set; }
}

var a = new Point { X = 1 };
var b = new Point { X = 1 };
Console.WriteLine(a == b);
''')
c("s4-030", r'''
public class BankAccountAggregate
{
    public void Withdraw(decimal amount) { ... CheckLimit(amount); ... }
    private void CheckLimit(decimal amount) { ... }
}

// test Withdraw's behaviour, not CheckLimit
''')
