# Cheat Sheet — Section 4: OOP, DDD & Testing

Projects: [4.1](../Section4_OopDddAndTesting/Project4.1_CustomNuGetPackage), [4.2](../Section4_OopDddAndTesting/Project4.2_DddBankingEngine), [4.3](../Section4_OopDddAndTesting/Project4.3_PlaywrightBrowserTests).
See also [WHY_THESE_TYPES.md](../WHY_THESE_TYPES.md#2-class-vs-record-vs-struct-vs-interface-vs-static-class-vs-abstract-class).

## Class, properties, constructor

```csharp
public class BankAccount
{
    private readonly List<string> history = new(); // private field

    public BankAccount(string owner)
    {
        Owner = owner;
    }

    public string Owner { get; }                    // read-only, set in constructor
    public decimal Balance { get; private set; }    // only this class can change it
    public bool IsEmpty => Balance == 0;            // calculated

    public void Deposit(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be more than 0.", nameof(amount));
        }
        Balance += amount;
        history.Add($"+{amount}");
    }
}
```

## Interface, abstract class, inheritance

```csharp
public interface IShape
{
    double Area();
}

public abstract class ShapeBase : IShape
{
    public abstract double Area();            // each child MUST write this

    public virtual string Describe()          // child MAY change this
    {
        return $"Area {Area():0.00}";
    }
}

public class Circle : ShapeBase
{
    public Circle(double radius)
    {
        Radius = radius;
    }

    public double Radius { get; }

    public override double Area()
    {
        return Math.PI * Radius * Radius;
    }
}

IShape shape = new Circle(2);                 // use through the interface
```

## Static class, extension method, generics

```csharp
public static class StringExtensions
{
    public static string Shorten(this string text, int maxLength)
    {
        return text.Length <= maxLength ? text : text[..maxLength] + "...";
    }
}
string preview = "A very long title".Shorten(6); // "A very..."

public class Box<T>
{
    public Box(T value)
    {
        Value = value;
    }

    public T Value { get; }
}

public static T Largest<T>(IEnumerable<T> items) where T : IComparable<T>
{
    return items.Max()!;
}
```

## DDD building blocks

```csharp
// VALUE OBJECT: no id, equal by value, never changes
public record Money(decimal Amount, string Currency);

// ENTITY / AGGREGATE ROOT: has an id; rules live inside; changes only through methods
public class Order
{
    private readonly List<OrderLine> lines = new();

    private Order(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; }
    public IReadOnlyList<OrderLine> Lines => lines;

    public static Order Create()
    {
        return new Order(Guid.NewGuid());
    }

    public void AddLine(string sku, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be more than 0.");
        }
        lines.Add(new OrderLine(sku, quantity));
    }
}

// DOMAIN EVENT: a past-tense fact
public record OrderPlacedEvent(Guid OrderId, DateTimeOffset HappenedAt);

// REPOSITORY: the domain says WHAT it needs
public interface IOrderRepository
{
    Order? FindById(Guid id);
    void Save(Order order);
}
```

## xUnit

```csharp
public class BankAccountTest
{
    [Fact]
    public void Deposit_PositivePositive_ValidAmount_RaisesBalance()
    {
        var account = new BankAccount("Ann");   // Arrange

        account.Deposit(25m);                   // Act

        Assert.Equal(25m, account.Balance);     // Assert
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Deposit_NegativeNegative_NotPositive_Throws(decimal amount)
    {
        var account = new BankAccount("Ann");

        Assert.Throws<ArgumentException>(() => account.Deposit(amount));
    }
}
```

| Assert | Checks |
|---|---|
| `Assert.Equal(expected, actual)` / `NotEqual` | values |
| `Assert.True(x)` / `False(x)` | a condition |
| `Assert.Null(x)` / `NotNull(x)` | null |
| `Assert.Empty(list)` / `NotEmpty` / `Single(list)` | collections |
| `Assert.Contains(item, list)` / `Contains("sub", text)` | contains |
| `Assert.Throws<T>(() => ...)` / `await Assert.ThrowsAsync<T>(...)` | exception |
| `Assert.Same(a, b)` | the very same object |

## Setup per test and shared setup

```csharp
public class AccountServiceTest : IDisposable
{
    private readonly AccountRepositoryFake repository = new();   // new for EVERY test

    public void Dispose()
    {
        // runs after every test
    }
}

public class WebsiteTest : IClassFixture<WebApplicationFactory<Program>>  // ONE shared for the class
{
    private readonly WebApplicationFactory<Program> factory;

    public WebsiteTest(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }
}
```

## A hand-written fake

```csharp
public class OrderRepositoryFake : IOrderRepository
{
    public Dictionary<Guid, Order> Saved { get; } = new();

    public Order? FindById(Guid id)
    {
        return Saved.GetValueOrDefault(id);
    }

    public void Save(Order order)
    {
        Saved[order.Id] = order;
    }
}
```

## Integration test (whole app in memory)

```csharp
HttpClient client = factory.CreateClient();
HttpResponseMessage response = await client.GetAsync("/products/1");
Assert.Equal(HttpStatusCode.OK, response.StatusCode);
string html = await response.Content.ReadAsStringAsync();
```

The app's `Program.cs` needs `public partial class Program { }` at the bottom.

## Playwright (real browser)

```csharp
IPlaywright playwright = await Playwright.CreateAsync();
IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
IPage page = await browser.NewPageAsync();
await page.GotoAsync("http://localhost:5000/Products/Create");
await page.FillAsync("#Name", "Lamp");
await page.SelectOptionAsync("#Category", "Lighting");
await page.ClickAsync("button[type=submit]");
string alert = await page.Locator("#success-alert").InnerTextAsync();
```

## NuGet package

```bash
dotnet pack MyLibrary -c Release              # makes MyLibrary.1.0.0.nupkg
dotnet add package MyLibrary --version 1.0.0  # use it in another project
dotnet nuget push MyLibrary.1.0.0.nupkg --source https://api.nuget.org/v3/index.json --api-key <key>
```
